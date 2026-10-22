using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Common;
using Core.Logging;
using Cysharp.Threading.Tasks;
using Infrastructure.MasterData;
using NUnit.Framework;

public sealed class MasterDatabaseInitializationTests
{
    private sealed class TestLogger : ILogger
    {
        public void Log(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
    }
    // This fake controls I/O only; tests exercise the production initialization policy.
    private sealed class Loader
    {
        public readonly List<string> Calls = new();
        public readonly object Core = new();
        public readonly object Ui = new();
        public string FailKey;
        public UniTaskCompletionSource<object> PendingCore;
        public UniTask<object> Load(string key)
        {
            Calls.Add(key);
            if (key == FailKey) throw new InvalidOperationException("Simulated missing asset");
            if (key == GameConstants.MasterCoreKey && PendingCore != null) return PendingCore.Task;
            return UniTask.FromResult(key == GameConstants.MasterCoreKey ? Core : Ui);
        }
    }
    [Test] public async Task CoreFailureReportsKeyAndCanBeRetried()
    {
        var loader = new Loader { FailKey = GameConstants.MasterCoreKey };
        var provider = new MasterDatabaseInitialization<object>(loader.Load, new TestLogger());
        try { await provider.InitializeAsync(); Assert.Fail("Expected failure"); }
        catch (MasterDataLoadException error)
        {
            Assert.That(error.Key, Is.EqualTo(GameConstants.MasterCoreKey));
            Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
        }
        Assert.That(provider.CoreDb, Is.Null);
        Assert.That(provider.UiDb, Is.Null);
        loader.FailKey = null; await provider.InitializeAsync();
        Assert.That(provider.CoreDb, Is.SameAs(loader.Core));
        Assert.That(provider.UiDb, Is.SameAs(loader.Ui));
        Assert.That(loader.Calls, Is.EqualTo(new[] { GameConstants.MasterCoreKey, GameConstants.MasterCoreKey, GameConstants.MasterUiKey }));
    }
    [Test] public async Task UiFailurePreservesCoreAndRetriesOnlyUi()
    {
        var loader = new Loader { FailKey = GameConstants.MasterUiKey };
        var provider = new MasterDatabaseInitialization<object>(loader.Load, new TestLogger());
        try { await provider.InitializeAsync(); Assert.Fail("Expected failure"); }
        catch (MasterDataLoadException error) { Assert.That(error.Key, Is.EqualTo(GameConstants.MasterUiKey)); }
        Assert.That(provider.CoreDb, Is.SameAs(loader.Core));
        Assert.That(provider.UiDb, Is.Null);
        loader.FailKey = null; await provider.InitializeAsync();
        await provider.InitializeAsync();
        Assert.That(loader.Calls, Is.EqualTo(new[] { GameConstants.MasterCoreKey, GameConstants.MasterUiKey, GameConstants.MasterUiKey }));
    }
    [Test] public async Task ConcurrentInitializationLoadsEachDatabaseOnce()
    {
        var loader = new Loader { PendingCore = new UniTaskCompletionSource<object>() };
        var provider = new MasterDatabaseInitialization<object>(loader.Load, new TestLogger());
        var first = provider.InitializeAsync(); var second = provider.InitializeAsync();
        Assert.That(loader.Calls.Count, Is.EqualTo(1));
        loader.PendingCore.TrySetResult(loader.Core);
        await first; await second;
        Assert.That(loader.Calls, Is.EqualTo(new[] { GameConstants.MasterCoreKey, GameConstants.MasterUiKey }));
        Assert.That(provider.UiDb, Is.SameAs(loader.Ui));
    }
    [Test] public async Task NullDatabaseIsFailureAndDoesNotPoisonRetry()
    {
        bool returnNull = true;
        var provider = new MasterDatabaseInitialization<object>(
            key => UniTask.FromResult(returnNull ? null : new object()), new TestLogger());
        try { await provider.InitializeAsync(); Assert.Fail("Expected failure"); }
        catch (MasterDataLoadException error) { Assert.That(error.Key, Is.EqualTo(GameConstants.MasterCoreKey)); }
        returnNull = false; await provider.InitializeAsync();
        Assert.That(provider.CoreDb, Is.Not.Null);
        Assert.That(provider.UiDb, Is.Not.Null);
    }
}


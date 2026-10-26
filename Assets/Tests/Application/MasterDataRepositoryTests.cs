using System.IO;
using Infrastructure.MasterData;
using Infrastructure.MasterData.Repositories;
using MasterMemory;
using NUnit.Framework;
using UnityEngine;

public sealed class MasterDataRepositoryTests
{
    private sealed class Provider : IMasterDatabaseProvider
    {
        public MemoryDatabase CoreDb { get; }
        public MemoryDatabase UiDb { get; }
        public bool IsCoreReady => true;
        public bool IsUiReady => true;

        public Provider()
        {
            CoreDb = Load("MasterData/Binary/master_core.bytes");
            UiDb = Load("MasterData/Binary/master_ui.bytes");
        }

        private static MemoryDatabase Load(string relativePath)
            => new(File.ReadAllBytes(Path.Combine(Application.dataPath, relativePath)));
    }

    private Provider _provider;

    [SetUp]
    public void SetUp() => _provider = new Provider();

    [Test]
    public void ExistingMasterDataIsReturned()
    {
        Assert.That(new CharacterRepository(_provider).Get(1).Name, Is.EqualTo("Hero"));
        Assert.That(new SkillRepository(_provider).Get(1).Name, Is.EqualTo("Slash"));
        Assert.That(new StageRepository(_provider).Get(1).DisplayName, Is.EqualTo("First Encounter"));
        Assert.That(new UIMasterRepository(_provider).Get("TitleScreen").PrefabKey,
            Is.EqualTo("UI/Title/TitleScreen"));
    }

    [TestCase("CharacterMaster", 999)]
    [TestCase("SkillMaster", 999)]
    [TestCase("StageMaster", 999)]
    public void MissingCoreMasterDataReportsTableAndKey(string tableName, int key)
    {
        MasterDataNotFoundException error = tableName switch
        {
            "CharacterMaster" => Assert.Throws<MasterDataNotFoundException>(
                () => new CharacterRepository(_provider).Get(key)),
            "SkillMaster" => Assert.Throws<MasterDataNotFoundException>(
                () => new SkillRepository(_provider).Get(key)),
            _ => Assert.Throws<MasterDataNotFoundException>(
                () => new StageRepository(_provider).Get(key)),
        };

        Assert.That(error.TableName, Is.EqualTo(tableName));
        Assert.That(error.Key, Is.EqualTo(key));
        Assert.That(error.Message, Does.Contain($"table={tableName}"));
        Assert.That(error.Message, Does.Contain($"key={key}"));
    }

    [Test]
    public void MissingUiMasterDataReportsTableAndKey()
    {
        const string key = "MissingUi";
        var error = Assert.Throws<MasterDataNotFoundException>(
            () => new UIMasterRepository(_provider).Get(key));

        Assert.That(error.TableName, Is.EqualTo("UIMaster"));
        Assert.That(error.Key, Is.EqualTo(key));
    }
}

using System;
using System.Collections.Generic;
using Application.Messages;
using Application.Services;
using Domain.Battle;
using Domain.Run;
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;
using MessagePipe;
using UnityEngine;
using Zenject;
using ILogger = Core.Logging.ILogger;

namespace Infrastructure.Telemetry
{
    public sealed class FirebaseGameTelemetry : IGameTelemetry, IInitializable, IDisposable
    {
        private const int MaxPendingEvents = 64;

        private readonly struct Value
        {
            public string Name { get; }
            public string Text { get; }
            public long? Number { get; }

            public Value(string name, string text)
            {
                Name = name;
                Text = text;
                Number = null;
            }

            public Value(string name, long number)
            {
                Name = name;
                Text = null;
                Number = number;
            }
        }

        private readonly struct PendingEvent
        {
            public string Name { get; }
            public Value[] Values { get; }

            public PendingEvent(string name, params Value[] values)
            {
                Name = name;
                Values = values;
            }
        }

        private readonly ILogger _logger;
        private readonly ISubscriber<BattleActionResolvedMessage> _actions;
        private readonly Queue<PendingEvent> _pending = new();
        private readonly Queue<(Exception Exception, string Context)> _pendingExceptions = new();
        private IDisposable _actionSubscription;
        private bool _ready;
        private bool _disposed;
        private bool _firebaseEnabled;

        public FirebaseGameTelemetry(ILogger logger, ISubscriber<BattleActionResolvedMessage> actions)
        {
            _logger = logger;
            _actions = actions;
        }

        public void Initialize()
        {
            _actionSubscription = _actions.Subscribe(OnActionResolved);
            _firebaseEnabled = UnityEngine.Application.isMobilePlatform;

            if (!_firebaseEnabled)
            {
                _logger.Log("Firebase telemetry is disabled outside Android/iOS builds.");
                return;
            }

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (_disposed) return;
                if (task.IsFaulted)
                {
                    _logger.Error($"Firebase dependency check failed: {task.Exception}");
                    return;
                }
                if (task.Result != DependencyStatus.Available)
                {
                    _logger.Error($"Firebase dependencies are unavailable: {task.Result}");
                    return;
                }

                _ = FirebaseApp.DefaultInstance;
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                Crashlytics.IsCrashlyticsCollectionEnabled = true;
                _ready = true;
                FlushPendingEvents();
                FlushPendingExceptions();
                _logger.Log("Firebase Analytics and Crashlytics initialized.");
            });
        }

        public void TrackRunStarted() => EnqueueOrSend(new PendingEvent("run_started"));

        public void TrackBattleStarted(int round, int enemyId) => EnqueueOrSend(
            new PendingEvent("battle_started", new Value("round", round), new Value("enemy_id", enemyId)));

        public void TrackBattleFinished(int round, bool won, int turns) => EnqueueOrSend(
            new PendingEvent("battle_finished", new Value("round", round), new Value("won", won ? 1 : 0),
                new Value("turns", turns)));

        public void TrackUpgradeSelected(RunUpgrade upgrade) => EnqueueOrSend(
            new PendingEvent("upgrade_selected", new Value("upgrade", upgrade.ToString().ToLowerInvariant())));

        public void TrackRunFinished(bool cleared, int wins, int totalTurns, int totalDamageTaken) => EnqueueOrSend(
            new PendingEvent("run_finished", new Value("cleared", cleared ? 1 : 0), new Value("wins", wins),
                new Value("total_turns", totalTurns), new Value("damage_taken", totalDamageTaken)));

        public void RecordException(Exception exception, string context)
        {
            if (exception == null) return;
            if (!_ready)
            {
                if (_firebaseEnabled)
                {
                    if (_pendingExceptions.Count == 16) _pendingExceptions.Dequeue();
                    _pendingExceptions.Enqueue((exception, context));
                }
                _logger.Error($"{context}: {exception}");
                return;
            }

            Crashlytics.Log(context ?? "Unhandled game error");
            Crashlytics.LogException(exception);
        }

        private void OnActionResolved(BattleActionResolvedMessage message)
        {
            var result = message.Result;
            if (result.Actor != BattleSide.Player) return;
            EnqueueOrSend(new PendingEvent("battle_action_selected",
                new Value("action", result.Action.ToString().ToLowerInvariant()),
                new Value("damage", result.Damage)));
        }

        private void EnqueueOrSend(PendingEvent telemetryEvent)
        {
            if (_disposed) return;
            if (_ready)
            {
                Send(telemetryEvent);
                return;
            }

            if (_pending.Count == MaxPendingEvents) _pending.Dequeue();
            _pending.Enqueue(telemetryEvent);
        }

        private void FlushPendingEvents()
        {
            while (_pending.Count > 0) Send(_pending.Dequeue());
        }

        private void FlushPendingExceptions()
        {
            while (_pendingExceptions.Count > 0)
            {
                var pending = _pendingExceptions.Dequeue();
                Crashlytics.Log(pending.Context ?? "Game error before Firebase initialization");
                Crashlytics.LogException(pending.Exception);
            }
        }

        private static void Send(PendingEvent telemetryEvent)
        {
            var parameters = new Parameter[telemetryEvent.Values.Length];
            for (var index = 0; index < parameters.Length; index++)
            {
                var value = telemetryEvent.Values[index];
                parameters[index] = value.Number.HasValue
                    ? new Parameter(value.Name, value.Number.Value)
                    : new Parameter(value.Name, value.Text ?? string.Empty);
            }
            FirebaseAnalytics.LogEvent(telemetryEvent.Name, parameters);
        }

        public void Dispose()
        {
            _disposed = true;
            _actionSubscription?.Dispose();
            _pending.Clear();
            _pendingExceptions.Clear();
        }
    }
}

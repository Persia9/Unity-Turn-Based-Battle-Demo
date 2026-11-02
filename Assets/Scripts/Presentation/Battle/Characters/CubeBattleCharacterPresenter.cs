using System;
using Application.Messages;
using Application.Services;
using Domain.Battle;
using MessagePipe;
using UnityEngine;
using Zenject;

namespace Presentation.Battle.Characters
{
    public sealed class CubeBattleCharacterPresenter : IInitializable, IDisposable
    {
        private readonly BattleCharacterAnchors _anchors;
        private readonly IBattleSession _session;
        private readonly ISubscriber<BattleActionResolvedMessage> _actions;
        private IDisposable _subscription;
        private CubeBattleCharacterView _player;
        private CubeBattleCharacterView _enemy;
        private GameObject _platform;

        public CubeBattleCharacterPresenter(BattleCharacterAnchors anchors, IBattleSession session,
            ISubscriber<BattleActionResolvedMessage> actions)
        {
            _anchors = anchors;
            _session = session;
            _actions = actions;
        }

        public void Initialize()
        {
            var state = _session.State;
            var playerName = string.IsNullOrWhiteSpace(state.Player.Name) ? "Player" : state.Player.Name;
            var enemyName = string.IsNullOrWhiteSpace(state.Enemy.Name) ? "Enemy" : state.Enemy.Name;
            _player = CubeBattleCharacterView.Create(_anchors.PlayerRoot, playerName, new Color(0.15f, 0.45f, 1f));
            _enemy = CubeBattleCharacterView.Create(_anchors.EnemyRoot, enemyName, new Color(1f, 0.25f, 0.18f));
            CreatePlatform();
            _subscription = _actions.Subscribe(OnActionResolved);
        }

        private void OnActionResolved(BattleActionResolvedMessage message)
        {
            var result = message.Result;
            var actor = result.Actor == BattleSide.Player ? _player : _enemy;
            var target = result.Target == BattleSide.Player ? _player : _enemy;
            var direction = target.transform.position - actor.transform.position;

            if (result.Action == BattleAction.Defend)
            {
                actor.PlayDefend();
                return;
            }

            actor.PlayAttack(result.Action == BattleAction.StrongAttack, direction);
            target.PlayHit(result.RemainingHp <= 0, direction);
        }

        private void CreatePlatform()
        {
            _platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _platform.name = "Cube Battle Platform";
            _platform.transform.SetParent(_anchors.PlayerRoot.parent, false);
            _platform.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            _platform.transform.localScale = new Vector3(8.5f, 0.3f, 4.5f);
            var collider = _platform.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.Destroy(collider);
            _platform.GetComponent<Renderer>().material.color = new Color(0.18f, 0.2f, 0.24f);
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            if (_player != null) UnityEngine.Object.Destroy(_player.gameObject);
            if (_enemy != null) UnityEngine.Object.Destroy(_enemy.gameObject);
            if (_platform != null)
            {
                var platformRenderer = _platform.GetComponent<Renderer>();
                if (platformRenderer != null && platformRenderer.material != null)
                    UnityEngine.Object.Destroy(platformRenderer.material);
                UnityEngine.Object.Destroy(_platform);
            }
        }
    }
}

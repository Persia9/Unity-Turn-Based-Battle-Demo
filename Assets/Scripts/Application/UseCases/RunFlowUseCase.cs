using System;
using Application.Messages;
using Application.Services;
using Core.Common;
using Cysharp.Threading.Tasks;
using Domain.Battle;
using Domain.Run;
using Infrastructure.MasterData.Repositories;
using Infrastructure.Scene;
using MessagePipe;

namespace Application.UseCases
{
    public sealed class RunFlowUseCase
    {
        private readonly RunSession _run;
        private readonly IBattleSession _battle;
        private readonly IStageRepository _stages;
        private readonly ICharacterRepository _characters;
        private readonly ISceneService _scenes;
        private readonly IPublisher<BattleStartedMessage> _started;
        private bool _loading;
        private bool _advancePending;
        private int _completedRevision = -1;
        public RunFlowUseCase(RunSession run, IBattleSession battle, IStageRepository stages,
            ICharacterRepository characters, ISceneService scenes, IPublisher<BattleStartedMessage> started)
        { _run = run; _battle = battle; _stages = stages; _characters = characters; _scenes = scenes; _started = started; }

        public async UniTask StartNewRun()
        {
            if (_loading) return;
            _loading = true;
            try
            {
                var first = _stages.Get(1);
                _run.State.Start(_characters.Get(first.PlayerCharacterId).MaxHp);
                _advancePending = false;
                await LoadCurrentBattle();
            }
            finally { _loading = false; }
        }
        public async UniTask ChooseUpgrade(RunUpgrade upgrade)
        {
            if (_loading) return;
            _loading = true;
            try
            {
                if (_run.State.Phase == RunPhase.ChoosingUpgrade)
                { _run.State.ChooseUpgrade(upgrade); _advancePending = true; }
                else if (!_advancePending) throw new InvalidOperationException("No upgrade is pending.");
                // On scene-load failure, retry without applying the upgrade twice.
                await LoadCurrentBattle();
                _advancePending = false;
            }
            finally { _loading = false; }
        }
        public void PrepareCurrentBattle()
        {
            if (_run.State.Phase == RunPhase.NotStarted)
                _run.State.Start(_characters.Get(_stages.Get(1).PlayerCharacterId).MaxHp);
            if (_run.State.Phase != RunPhase.InBattle) throw new InvalidOperationException("Run cannot enter a battle now.");
            var stage = _stages.Get(_run.State.Round);
            var player = _characters.Get(stage.PlayerCharacterId);
            var enemy = _characters.Get(stage.EnemyCharacterId);
            _battle.Initialize(new BattleUnitSnapshot(player.Id, player.Name, _run.State.MaxHp, _run.State.Hp, player.Attack),
                new BattleUnitSnapshot(enemy.Id, enemy.Name, enemy.MaxHp, enemy.MaxHp, enemy.Attack),
                (EnemyAttackPattern)stage.EnemyPattern, _run.State.DamageBonus, _run.State.GuardDamagePercent);
        }
        public bool CompleteBattle()
        {
            if (!_battle.State.IsFinished || _completedRevision == _battle.Revision) return false;
            if (_run.State.Phase != RunPhase.InBattle) return false;
            _run.State.CompleteBattle(_battle.State.Winner == BattleSide.Player,
                _battle.State.Player.Hp, _battle.State.TurnCount);
            _completedRevision = _battle.Revision;
            return true;
        }
        private async UniTask LoadCurrentBattle()
        {
            PrepareCurrentBattle();
            await _scenes.LoadScene(GameConstants.BattleSceneName);
            _started.Publish(new BattleStartedMessage(_battle.State.Player.CharacterId, _battle.State.Enemy.CharacterId));
        }
    }
}

using System;
using Application.Services;
using Core.Common;
using Cysharp.Threading.Tasks;
using Domain.Battle;
using Domain.Run;
using Infrastructure.MasterData.Repositories;
using Infrastructure.Scene;

namespace Application.UseCases
{
    public sealed class RunFlowUseCase
    {
        private readonly RunSession _run;
        private readonly IBattleSession _battle;
        private readonly IStageRepository _stages;
        private readonly ICharacterRepository _characters;
        private readonly ISkillRepository _skills;
        private readonly ISceneService _scenes;
        private readonly IGameTelemetry _telemetry;
        private bool _loading;
        private bool _advancePending;
        private int _completedRevision = -1;
        public RunFlowUseCase(RunSession run, IBattleSession battle, IStageRepository stages,
            ICharacterRepository characters, ISkillRepository skills, ISceneService scenes,
            IGameTelemetry telemetry = null)
        {
            _run = run; _battle = battle; _stages = stages; _characters = characters; _skills = skills;
            _scenes = scenes; _telemetry = telemetry;
        }

        public async UniTask StartNewRun()
        {
            if (_loading) return;
            _loading = true;
            try
            {
                var first = _stages.Get(1);
                _run.State.Start(_characters.Get(first.PlayerCharacterId).MaxHp);
                _telemetry?.TrackRunStarted();
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
                {
                    _run.State.ChooseUpgrade(upgrade);
                    _telemetry?.TrackUpgradeSelected(upgrade);
                    _advancePending = true;
                }
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
            var playerSkill = _skills.Get(player.DefaultSkillId);
            var enemySkill = _skills.Get(enemy.DefaultSkillId);
            _battle.Initialize(new BattleUnitSnapshot(player.Id, player.Name, _run.State.MaxHp, _run.State.Hp, playerSkill.Power),
                new BattleUnitSnapshot(enemy.Id, enemy.Name, enemy.MaxHp, enemy.MaxHp, enemySkill.Power),
                (EnemyAttackPattern)stage.EnemyPattern, _run.State.DamageBonus, _run.State.GuardDamagePercent);
        }
        public bool CompleteBattle()
        {
            if (!_battle.State.IsFinished || _completedRevision == _battle.Revision) return false;
            if (_run.State.Phase != RunPhase.InBattle) return false;
            var won = _battle.State.Winner == BattleSide.Player;
            var completedRound = _run.State.Round;
            _run.State.CompleteBattle(won, _battle.State.Player.Hp, _battle.State.TurnCount);
            _telemetry?.TrackBattleFinished(completedRound, won, _battle.State.TurnCount);
            if (_run.State.Phase is RunPhase.Cleared or RunPhase.Defeated)
                _telemetry?.TrackRunFinished(_run.State.Phase == RunPhase.Cleared, _run.State.Wins,
                    _run.State.TotalTurns, _run.State.TotalDamageTaken);
            _completedRevision = _battle.Revision;
            return true;
        }
        private async UniTask LoadCurrentBattle()
        {
            PrepareCurrentBattle();
            await _scenes.LoadScene(GameConstants.BattleSceneName);
            _telemetry?.TrackBattleStarted(_run.State.Round, _battle.State.Enemy.CharacterId);
        }
    }
}

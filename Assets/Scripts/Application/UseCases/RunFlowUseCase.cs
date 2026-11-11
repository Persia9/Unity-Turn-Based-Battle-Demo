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
        private bool _loading;
        private bool _advancePending;
        private int _completedRevision = -1;
        public RunFlowUseCase(RunSession run, IBattleSession battle, IStageRepository stages,
            ICharacterRepository characters, ISkillRepository skills, ISceneService scenes)
        { _run = run; _battle = battle; _stages = stages; _characters = characters; _skills = skills; _scenes = scenes; }

        public async UniTask StartNewRun(RunMode mode = RunMode.Expedition)
        {
            if (_loading) return;
            _loading = true;
            try
            {
                ResetRun(mode);
                await LoadCurrentBattle();
            }
            finally { _loading = false; }
        }
        public void RestartInPlace(RunMode mode)
        {
            if (_loading) throw new InvalidOperationException("A battle transition is already in progress.");
            ResetRun(mode);
            PrepareCurrentBattle();
        }

        private void ResetRun(RunMode mode)
        {
            var first = _stages.Get(1);
            _run.State.Start(_characters.Get(first.PlayerCharacterId).MaxHp, mode);
            _advancePending = false;
        }

        public void ChooseUpgrade(RunUpgrade upgrade)
        {
            if (_loading) throw new InvalidOperationException("A battle transition is already in progress.");
            if (_run.State.Phase == RunPhase.ChoosingUpgrade)
            {
                _run.State.ChooseUpgrade(upgrade);
                _advancePending = true;
            }
            else if (!_advancePending)
            {
                throw new InvalidOperationException("No upgrade is pending.");
            }

            // If preparation fails, retry without applying the upgrade twice.
            PrepareCurrentBattle();
            _advancePending = false;
        }

        public void PrepareCurrentBattle()
        {
            if (_run.State.Phase == RunPhase.NotStarted)
                _run.State.Start(_characters.Get(_stages.Get(1).PlayerCharacterId).MaxHp);
            if (_run.State.Phase != RunPhase.InBattle) throw new InvalidOperationException("Run cannot enter a battle now.");
            var stage = _stages.Get(CurrentStageId());
            var player = _characters.Get(stage.PlayerCharacterId);
            var enemy = _characters.Get(stage.EnemyCharacterId);
            var playerSkill = _skills.Get(player.DefaultSkillId);
            var enemySkill = _skills.Get(enemy.DefaultSkillId);
            var enemyHp = _run.State.Mode == RunMode.Endless
                ? EndlessDifficulty.ScaleHp(enemy.MaxHp, _run.State.Round)
                : enemy.MaxHp;
            var enemyPower = _run.State.Mode == RunMode.Endless
                ? EndlessDifficulty.ScalePower(enemySkill.Power, _run.State.Round)
                : enemySkill.Power;
            _battle.Initialize(new BattleUnitSnapshot(player.Id, player.Name, _run.State.MaxHp, _run.State.Hp, playerSkill.Power),
                new BattleUnitSnapshot(enemy.Id, enemy.Name, enemyHp, enemyHp, enemyPower),
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
        }

        private int CurrentStageId()
        {
            if (_run.State.Mode == RunMode.Expedition) return _run.State.Round;
            if (EndlessDifficulty.IsBossRound(_run.State.Round)) return RunRules.ExpeditionBattleCount;
            return ((_run.State.Round - 1) % (RunRules.ExpeditionBattleCount - 1)) + 1;
        }
    }
}

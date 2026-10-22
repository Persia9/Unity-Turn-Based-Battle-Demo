using Application.UseCases;
using Application.Services;
using Cysharp.Threading.Tasks;
using Domain.Battle;
using Infrastructure.MasterData.Repositories;
using Infrastructure.UI;
using Presentation.Battle;
using Zenject;

namespace SceneFlow.Battle
{
    public sealed class BattleSceneEntryPoint : IInitializable
    {
        private readonly IUIService _ui;
        private readonly InitializeMasterDataUseCase _initialize;
        private readonly IBattleSession _session;
        private readonly IStageRepository _stages;
        private readonly ICharacterRepository _characters;
        public BattleSceneEntryPoint(IUIService ui, InitializeMasterDataUseCase initialize,
            IBattleSession session, IStageRepository stages, ICharacterRepository characters)
        { _ui = ui; _initialize = initialize; _session = session; _stages = stages; _characters = characters; }
        public void Initialize() => Run().Forget();
        private async UniTask Run()
        {
            await _initialize.Execute();
            // Support playing the Battle scene directly in the Editor.
            if (_session.State.TurnCount == 0 || _session.State.IsFinished)
            {
                var stage = _stages.Get(1);
                var player = _characters.Get(stage.PlayerCharacterId);
                var enemy = _characters.Get(stage.EnemyCharacterId);
                _session.Initialize(new BattleUnitSnapshot(player.Id, player.Name, player.MaxHp, player.MaxHp, player.Attack),
                    new BattleUnitSnapshot(enemy.Id, enemy.Name, enemy.MaxHp, enemy.MaxHp, enemy.Attack));
            }
            await _ui.Open<BattleHudView>("BattleHud");
        }
    }
}

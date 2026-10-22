using Application.Services;
using Application.UseCases;
using Cysharp.Threading.Tasks;
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
        private readonly RunFlowUseCase _flow;
        public BattleSceneEntryPoint(IUIService ui, InitializeMasterDataUseCase initialize,
            IBattleSession session, RunFlowUseCase flow)
        { _ui = ui; _initialize = initialize; _session = session; _flow = flow; }
        public void Initialize() => Run().Forget();
        private async UniTask Run()
        {
            await _initialize.Execute();
            if (_session.State.TurnCount == 0) _flow.PrepareCurrentBattle();
            await _ui.Open<BattleHudView>("BattleHud");
        }
    }
}

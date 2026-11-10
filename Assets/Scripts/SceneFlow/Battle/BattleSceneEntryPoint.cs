using Application.Services;
using Application.UseCases;
using Cysharp.Threading.Tasks;
using Infrastructure.UI;
using Infrastructure.Scene;
using Presentation.Battle;
using Zenject;

namespace SceneFlow.Battle
{
    public sealed class BattleSceneEntryPoint : IInitializable
    {
        private readonly ISceneLoadingOverlay _loadingOverlay;
        private readonly IUIService _ui;
        private readonly InitializeMasterDataUseCase _initialize;
        private readonly IBattleSession _session;
        private readonly RunFlowUseCase _flow;
        public BattleSceneEntryPoint(IUIService ui, InitializeMasterDataUseCase initialize,
            IBattleSession session, RunFlowUseCase flow, ISceneLoadingOverlay loadingOverlay)
        { _ui = ui; _initialize = initialize; _session = session; _flow = flow; _loadingOverlay = loadingOverlay; }
        public void Initialize()
        {
            _loadingOverlay.Show();
            Run().Forget();
        }
        private async UniTask Run()
        {
            try
            {
                await _initialize.Execute();
                if (_session.State.TurnCount == 0) _flow.PrepareCurrentBattle();
                await _ui.Open<BattleHudView>("BattleHud");
                // UI のレイアウト更新が完了するまでローディング表示を維持する。
                await UniTask.NextFrame();
                _loadingOverlay.Hide();
            }
            catch
            {
                _loadingOverlay.Hide();
                throw;
            }
        }
    }
}

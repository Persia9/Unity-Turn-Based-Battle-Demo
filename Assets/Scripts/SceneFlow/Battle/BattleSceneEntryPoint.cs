using Application.UseCases;
using Cysharp.Threading.Tasks;
using Infrastructure.UI;
using Presentation.Battle;
using Zenject;

namespace SceneFlow.Battle
{
    public sealed class BattleSceneEntryPoint : IInitializable
    {
        private readonly IUIService _uiService;
        private readonly StartBattleUseCase _startBattle;

        public BattleSceneEntryPoint(StartBattleUseCase startBattle, IUIService uiService)
        {
            _startBattle = startBattle;
            _uiService = uiService;
        }
        
        public void Initialize()
        {
            Run().Forget();
        }

        private async UniTask Run()
        {
            await _uiService.Open<BattleHudView>("BattleHud");
        }
    }
}
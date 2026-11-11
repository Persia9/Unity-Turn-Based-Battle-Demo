using Startup;
using System;
using Application.Services;
using Application.UseCases;
using Cysharp.Threading.Tasks;
using Infrastructure.UI;
using Presentation.Battle;
using Presentation.Battle.Characters;
using Domain.Run;
using Zenject;

namespace SceneFlow.Battle
{
    public sealed class BattleSceneEntryPoint : IInitializable
    {
        private readonly ILoadingScreen _screen;
        private readonly StartupReady _ready;
        private readonly IUIService _ui;
        private readonly InitializeMasterDataUseCase _initialize;
        private readonly IBattleSession _session;
        private readonly RunFlowUseCase _flow;
        private readonly BattleViewModel _viewModel;
        private readonly CubeBattleCharacterPresenter _characters;
        private readonly JrpgBattleCameraPresenter _camera;

        public BattleSceneEntryPoint(IUIService ui, InitializeMasterDataUseCase initialize,
            IBattleSession session, RunFlowUseCase flow, ILoadingScreen screen, StartupReady ready,
            BattleViewModel viewModel, CubeBattleCharacterPresenter characters, JrpgBattleCameraPresenter camera)
        {
            _ui = ui;
            _initialize = initialize;
            _session = session;
            _flow = flow;
            _screen = screen;
            _ready = ready;
            _viewModel = viewModel;
            _characters = characters;
            _camera = camera;
        }

        public UniTask Retry(RunMode mode)
        {
            _flow.RestartInPlace(mode);
            return ShowNextBattle();
        }

        public UniTask ChooseUpgrade(RunUpgrade upgrade)
        {
            _flow.ChooseUpgrade(upgrade);
            return ShowNextBattle();
        }

        private async UniTask ShowNextBattle()
        {
            _camera.ResetForBattle();
            _characters.ResetForBattle();
            _viewModel.ResetForBattle();
            await _ui.Close("ResultPopup");
        }

        public void Initialize() => Run().Forget();
        private async UniTask Run()
        {
            try
            {
                await _initialize.Execute();
                if (_session.State.TurnCount == 0) _flow.PrepareCurrentBattle();
                await _ui.Open<BattleHudView>("BattleHud");
                await UniTask.NextFrame();
                _screen.Hide();
                _ready.Complete();
            }
            catch (Exception error)
            {
                _ready.Fail(error);
                _screen.Fail(error);
            }
        }
    }
}

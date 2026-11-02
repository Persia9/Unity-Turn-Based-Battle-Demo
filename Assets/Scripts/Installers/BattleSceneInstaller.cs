using Application.UseCases;
using Presentation.Battle;
using Presentation.Battle.Characters;
using Presentation.Battle.Performance;
using SceneFlow.Battle;
using UnityEngine;

namespace Installers
{
    public class BattleSceneInstaller : SceneInstaller
    {
        [SerializeField] private Transform _playerRoot;
        [SerializeField] private Transform _enemyRoot;

        public override void InstallBindings()
        {
            base.InstallBindings();
            Container.BindInstance(new BattleCharacterAnchors(_playerRoot, _enemyRoot));
            Container.BindInterfacesAndSelfTo<BattleViewModel>().AsSingle();
            Container.Bind<ITurnDelay>().To<UniTaskTurnDelay>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayPlayerTurnUseCase>().AsSingle();

            Container.BindInterfacesAndSelfTo<BattleSceneEntryPoint>().AsSingle();
            Container.BindInterfacesAndSelfTo<BattleEventPresenter>().AsSingle();
            Container.BindInterfacesTo<JrpgBattleCameraPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<CubeBattleCharacterPresenter>().AsSingle();
            Container.Bind<PerformanceView>().FromNewComponentOnNewGameObject()
                .WithGameObjectName("Performance View").AsSingle().NonLazy();
        }
    }
}

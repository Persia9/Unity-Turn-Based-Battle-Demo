using Application.UseCases;
using Application.Services;
using Infrastructure.Platform;
using Presentation.Battle.Result;
using Presentation.Battle.Performance;
using Presentation.Battle;
using Presentation.Battle.Characters;
using SceneFlow.Battle;
using UnityEngine;

namespace Installers
{
    public class BattleSceneInstaller : SceneInstaller
    {
        [SerializeField] private Transform _playerRoot;
        [SerializeField] private Transform _enemyRoot;
        [SerializeField] private CubeBattleCharacterView _playerPrefab;
        [SerializeField] private CubeBattleCharacterView _enemyPrefab;
        [SerializeField] private GameObject _platformPrefab;

        public override void InstallBindings()
        {
            base.InstallBindings();
            Container.BindInterfacesAndSelfTo<ResultPopupViewModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<PerformanceViewModel>().AsSingle();
            Container.Bind<IPerformanceMetrics>().To<UnityPerformanceMetrics>().AsSingle();
            Container.BindInstance(new BattleCharacterAnchors(_playerRoot, _enemyRoot,
                _playerPrefab, _enemyPrefab, _platformPrefab));
            Container.BindInterfacesAndSelfTo<BattleViewModel>().AsSingle();
            Container.Bind<ITurnDelay>().To<UniTaskTurnDelay>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayPlayerTurnUseCase>().AsSingle();

            Container.BindInterfacesAndSelfTo<BattleSceneEntryPoint>().AsSingle();
            Container.BindInterfacesAndSelfTo<BattleEventPresenter>().AsSingle();
            Container.BindInterfacesTo<JrpgBattleCameraPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<CubeBattleCharacterPresenter>().AsSingle();
        }
    }
}
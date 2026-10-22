using Application.UseCases;
using Presentation.Battle;
using SceneFlow.Battle;

namespace Installers
{
    public class BattleSceneInstaller : SceneInstaller
    {
        public override void InstallBindings()
        {
            base.InstallBindings();
            Container.BindInterfacesAndSelfTo<BattleViewModel>().AsSingle();
            Container.Bind<ITurnDelay>().To<UniTaskTurnDelay>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayPlayerTurnUseCase>().AsSingle();

            Container.BindInterfacesAndSelfTo<BattleSceneEntryPoint>().AsSingle();



            Container.BindInterfacesAndSelfTo<BattleEventPresenter>().AsSingle();
        }
    }
}

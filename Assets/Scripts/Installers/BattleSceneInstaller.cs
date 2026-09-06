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

            Container.BindInterfacesAndSelfTo<BattleSceneEntryPoint>().AsSingle();

            Container.Bind<OpenResultPopupUseCase>().AsSingle();

            Container.BindInterfacesAndSelfTo<BattleEventPresenter>().AsSingle();
        }
    }
}
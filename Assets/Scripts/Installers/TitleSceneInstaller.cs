using SceneFlow.Title;
using Presentation.Title;

namespace Installers
{
    public class TitleSceneInstaller : SceneInstaller
    {
        public override void InstallBindings()
        {
            base.InstallBindings();
            Container.BindInterfacesAndSelfTo<TitleScreenViewModel>().AsSingle();

            Container.BindInterfacesAndSelfTo<TitleSceneEntryPoint>().AsSingle();
        }
    }
}
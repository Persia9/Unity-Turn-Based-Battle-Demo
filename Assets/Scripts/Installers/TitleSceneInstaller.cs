using SceneFlow.Title;

namespace Installers
{
    public class TitleSceneInstaller : SceneInstaller
    {
        public override void InstallBindings()
        {
            base.InstallBindings();

            Container.BindInterfacesAndSelfTo<TitleSceneEntryPoint>().AsSingle();
        }
    }
}
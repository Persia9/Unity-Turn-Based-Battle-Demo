using Infrastructure.UI;
using UnityEngine;
using Zenject;

namespace Installers
{
    public abstract class SceneInstaller : MonoInstaller
    {
        [SerializeField] private UIRoot _uiRoot;

        public override void InstallBindings()
        {
            Container.Bind<UIRegistry>().AsSingle();
            Container.Bind<IUIViewFactory>().To<UIViewFactory>().AsSingle();
            Container.Bind<IUILayerService>()
                .To<UILayerService>()
                .AsSingle()
                .WithArguments(
                    _uiRoot.HudRoot,
                    _uiRoot.ScreenRoot,
                    _uiRoot.PopupRoot,
                    _uiRoot.OverlayRoot,
                    _uiRoot.TopRoot);

            Container.BindInterfacesTo<UIService>().AsSingle();
            Container.Bind<IPopupService>().To<PopupService>().AsSingle();
            Container.Bind<IScreenService>().To<ScreenService>().AsSingle();

        }
    }
}

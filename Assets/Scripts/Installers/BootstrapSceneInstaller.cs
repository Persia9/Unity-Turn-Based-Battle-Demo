using Infrastructure.UI;
using Presentation.Bootstrap;
using UnityEngine;
using Zenject;

namespace Installers
{
    public sealed class BootstrapSceneInstaller : MonoInstaller
    {
        [SerializeField] private UIRoot _uiRoot;
        [SerializeField] private BootstrapView _viewPrefab;
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<BootstrapViewModel>().AsSingle();
            Container.Bind<BootstrapView>().FromComponentInNewPrefab(_viewPrefab)
                .UnderTransform(_uiRoot.ScreenRoot).AsSingle().NonLazy();
        }
    }
}

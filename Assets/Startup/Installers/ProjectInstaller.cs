using Core.Logging;
using Startup;
using UnityEngine;
using Zenject;
using ILogger = Core.Logging.ILogger;

namespace Installers
{
    public sealed class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private LoadingScreen _screenPrefab;

        public override void InstallBindings()
        {
            Container.Bind<ILogger>().To<UnityLogger>().AsSingle();
            Container.Bind<StartupReady>().AsSingle();
            Container.Bind<ContentVersion>().AsSingle();
            Container.Bind(typeof(LoadingScreen), typeof(ILoadingScreen))
                .To<LoadingScreen>().FromComponentInNewPrefab(_screenPrefab)
                .UnderTransform(transform).AsSingle();
        }
    }
}
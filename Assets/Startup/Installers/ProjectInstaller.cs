using Core.Logging;
using Startup;
using UnityEngine;
using Zenject;
using ILogger = Core.Logging.ILogger;

namespace Installers
{
    public sealed class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private StartupScreen _screenPrefab;

        public override void InstallBindings()
        {
            Container.Bind<ILogger>().To<UnityLogger>().AsSingle();
            Container.Bind<StartupReady>().AsSingle();
            Container.Bind(typeof(StartupScreen), typeof(ILoadingScreen))
                .To<StartupScreen>().FromComponentInNewPrefab(_screenPrefab)
                .UnderTransform(transform).AsSingle();
        }
    }
}
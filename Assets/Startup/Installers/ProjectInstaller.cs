using Core.Logging;
using Zenject;
using ILogger = Core.Logging.ILogger;

namespace Installers
{
    public sealed class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<ILogger>().To<UnityLogger>().AsSingle();
        }
    }
}

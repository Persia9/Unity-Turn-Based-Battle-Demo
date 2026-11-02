using Application.Messages;
using Application.Services;
using Application.UseCases;
using Core.Logging;
using Domain.Battle;
using Infrastructure.Asset;
using Infrastructure.MasterData;
using Infrastructure.MasterData.Repositories;
using Infrastructure.Platform;
using Infrastructure.Scene;
using Infrastructure.Telemetry;
using Infrastructure.UI;
using MessagePipe;
using Zenject;
using ILogger = Core.Logging.ILogger;

namespace Installers
{
    public sealed class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<ILogger>().To<UnityLogger>().AsSingle();
            Container.BindInterfacesTo<MobileRuntimeSettings>().AsSingle();
            Container.BindInterfacesTo<FirebaseGameTelemetry>().AsSingle();

            Container.Bind<IAssetService>().To<AddressableAssetService>().AsSingle();
            Container.Bind<ISceneService>().To<UnitySceneService>().AsSingle();

            Container.Bind<IMasterDatabaseLoader>().To<AddressableMasterDatabaseLoader>().AsSingle();
            Container.BindInterfacesAndSelfTo<AddressableMasterDatabaseProvider>().AsSingle();

            Container.Bind<ICharacterRepository>().To<CharacterRepository>().AsSingle();
            Container.Bind<ISkillRepository>().To<SkillRepository>().AsSingle();
            Container.Bind<IStageRepository>().To<StageRepository>().AsSingle();
            Container.Bind<IUIMasterRepository>().To<UIMasterRepository>().AsSingle();


            Container.Bind<IUIDefinitionRepository>().To<UIDefinitionRepository>().AsSingle();

            Container.Bind<IBattleSession>().To<BattleSession>().AsSingle();
            Container.Bind<BattleSystem>().AsSingle();
            Container.Bind<RunSession>().AsSingle();
            Container.Bind<RunFlowUseCase>().AsSingle();

            Container.Bind<InitializeMasterDataUseCase>().AsSingle();
            Container.Bind<AttackUseCase>().AsSingle();
            Container.Bind<ReturnToTitleUseCase>().AsSingle();

            // MessagePipe
            var options = Container.BindMessagePipe();
            Container.BindMessageBroker<DamageAppliedMessage>(options);
            Container.BindMessageBroker<BattleActionResolvedMessage>(options);
            Container.BindMessageBroker<TurnChangedMessage>(options);
            Container.BindMessageBroker<BattleFinishedMessage>(options);

        }
    }
}

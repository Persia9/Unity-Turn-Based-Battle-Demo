using Application.Messages;
using Application.Services;
using Application.UseCases;
using Domain.Battle;
using Infrastructure.MasterData;
using Infrastructure.MasterData.Repositories;
using Infrastructure.Localization;
using Infrastructure.Platform;
using Infrastructure.Scene;
using Infrastructure.UI;
using MessagePipe;
using Zenject;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine.Scripting;


namespace HotUpdate
{
    [Preserve]
    public static class GameEntry
    {
        [Preserve]
        public static async UniTask Start(DiContainer container)
        {
            var existingDisposables = container.ResolveAll<IDisposable>();
            Install(container);
            var manager = container.Resolve<DisposableManager>();
            foreach (var disposable in container.ResolveAll<IDisposable>())
                if (!existingDisposables.Contains(disposable) && !ReferenceEquals(disposable, manager))
                    manager.Add(disposable);
            await container.Resolve<ISceneService>().LoadScene(Core.Common.GameConstants.TitleSceneName);
        }

        private static void Install(DiContainer Container)
        {
            Container.Bind<ILocalizationService>().To<LocalizationService>().AsSingle();
            new MobileRuntimeSettings().Initialize();

            Container.Bind<ISceneService>().To<SceneService>().AsSingle();

            Container.Bind<IMasterDatabaseLoader>().To<AssetBundleMasterDatabaseLoader>().AsSingle();
            Container.BindInterfacesAndSelfTo<MasterDatabaseProvider>().AsSingle();

            Container.Bind<ILocalizationRepository>().To<LocalizationRepository>().AsSingle();
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
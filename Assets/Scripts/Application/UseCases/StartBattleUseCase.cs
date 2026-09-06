using Application.Messages;
using Application.Services;
using Cysharp.Threading.Tasks;
using Domain.Battle;
using Infrastructure.MasterData.Repositories;
using Infrastructure.Scene;
using MessagePipe;

namespace Application.UseCases
{
    public sealed class StartBattleUseCase
    {
        private readonly ISceneService _sceneService;
        private readonly IStageRepository _stageRepository;
        private readonly ICharacterRepository _characterRepository;
        private readonly IBattleSession _battleSession;
        private readonly IPublisher<BattleStartedMessage> _battleStartedPublisher;

        public StartBattleUseCase(
            ISceneService sceneService,
            IStageRepository stageRepository,
            ICharacterRepository characterRepository,
            IBattleSession battleSession,
            IPublisher<BattleStartedMessage> battleStartedPublisher)
        {
            _sceneService = sceneService;
            _stageRepository = stageRepository;
            _characterRepository = characterRepository;
            _battleSession = battleSession;
            _battleStartedPublisher = battleStartedPublisher;
        }

        public async UniTask Execute(int stageId)
        {
            var stage = _stageRepository.Get(stageId);
            var player = _characterRepository.Get(stage.PlayerCharacterId);
            var enemy = _characterRepository.Get(stage.EnemyCharacterId);

            _battleSession.Initialize(
                new BattleUnitSnapshot(player.Id, player.Name, player.MaxHp, player.MaxHp, player.Attack),
                new BattleUnitSnapshot(enemy.Id, enemy.Name, enemy.MaxHp, enemy.MaxHp, enemy.Attack));

            await _sceneService.LoadScene(Core.Common.GameConstants.BattleSceneName);

            _battleStartedPublisher.Publish(new BattleStartedMessage(player.Id, enemy.Id));
        }
    }
}
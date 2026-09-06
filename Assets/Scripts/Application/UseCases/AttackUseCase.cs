using Application.Messages;
using Application.Services;
using Domain.Battle;
using Infrastructure.MasterData.Repositories;
using MessagePipe;

namespace Application.UseCases
{
    public sealed class AttackUseCase
    {
        private readonly IBattleSession _battleSession;
        private readonly BattleSystem _battleSystem;
        private readonly ISkillRepository _skillRepository;
        private readonly ICharacterRepository _characterRepository;
        private readonly IPublisher<DamageAppliedMessage> _damagePublisher;
        private readonly IPublisher<TurnChangedMessage> _turnPublisher;
        private readonly IPublisher<BattleFinishedMessage> _finishPublisher;
        private readonly IPublisher<PlaySeRequestedMessage> _sePublisher;

        public AttackUseCase(
            IBattleSession battleSession,
            BattleSystem battleSystem,
            ISkillRepository skillRepository,
            ICharacterRepository characterRepository,
            IPublisher<DamageAppliedMessage> damagePublisher,
            IPublisher<TurnChangedMessage> turnPublisher,
            IPublisher<BattleFinishedMessage> finishPublisher,
            IPublisher<PlaySeRequestedMessage> sePublisher)
        {
            _battleSession = battleSession;
            _battleSystem = battleSystem;
            _skillRepository = skillRepository;
            _characterRepository = characterRepository;
            _damagePublisher = damagePublisher;
            _turnPublisher = turnPublisher;
            _finishPublisher = finishPublisher;
            _sePublisher = sePublisher;
        }

        public void ExecutePlayerAttack()
        {
            var state = _battleSession.State;
            if (state.IsFinished || state.CurrentTurn != TurnOwner.Player) return;

            var character = _characterRepository.Get(state.Player.CharacterId);
            var skill = _skillRepository.Get(character.DefaultSkillId);

            var damage = _battleSystem.CalculateDamage(skill.Power);
            state.ApplyDamage(BattleSide.Enemy, damage);

            _sePublisher.Publish(new PlaySeRequestedMessage(skill.SeEventId));
            _damagePublisher.Publish(new DamageAppliedMessage(BattleSide.Enemy, damage, state.Enemy.Hp));

            if (state.IsFinished)
            {
                _finishPublisher.Publish(new BattleFinishedMessage(state.Winner.Value));
                return;
            }

            state.NextTurn();
            _turnPublisher.Publish(new TurnChangedMessage(state.CurrentTurn, state.TurnCount));
        }

        public void ExecuteEnemyAttack()
        {
            var state = _battleSession.State;
            if (state.IsFinished || state.CurrentTurn != TurnOwner.Enemy) return;

            var character = _characterRepository.Get(state.Enemy.CharacterId);
            var skill = _skillRepository.Get(character.DefaultSkillId);

            var damage = _battleSystem.CalculateDamage(skill.Power);
            state.ApplyDamage(BattleSide.Player, damage);

            _sePublisher.Publish(new PlaySeRequestedMessage(skill.SeEventId));
            _damagePublisher.Publish(new DamageAppliedMessage(BattleSide.Player, damage, state.Player.Hp));

            if (state.IsFinished)
            {
                _finishPublisher.Publish(new BattleFinishedMessage(state.Winner.Value));
                return;
            }

            state.NextTurn();
            _turnPublisher.Publish(new TurnChangedMessage(state.CurrentTurn, state.TurnCount));
        }
    }
}
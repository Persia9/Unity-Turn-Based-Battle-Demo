using Application.Messages;
using Application.Services;
using Domain.Battle;
using Infrastructure.MasterData.Repositories;
using MessagePipe;

namespace Application.UseCases
{
    public sealed class AttackUseCase
    {
        private readonly IBattleSession _session;
        private readonly BattleSystem _rules;
        private readonly ISkillRepository _skills;
        private readonly ICharacterRepository _characters;
        private readonly IPublisher<DamageAppliedMessage> _damage;
        private readonly IPublisher<TurnChangedMessage> _turn;
        private readonly IPublisher<BattleFinishedMessage> _finish;
        private bool _executing;

        public AttackUseCase(IBattleSession battleSession, BattleSystem battleSystem,
            ISkillRepository skillRepository, ICharacterRepository characterRepository,
            IPublisher<DamageAppliedMessage> damagePublisher, IPublisher<TurnChangedMessage> turnPublisher,
            IPublisher<BattleFinishedMessage> finishPublisher)
        {
            _session = battleSession; _rules = battleSystem; _skills = skillRepository;
            _characters = characterRepository; _damage = damagePublisher;
            _turn = turnPublisher; _finish = finishPublisher;
        }

        public bool ExecutePlayerAttack() => Execute(TurnOwner.Player, BattleSide.Enemy);
        public bool ExecuteEnemyAttack() => Execute(TurnOwner.Enemy, BattleSide.Player);

        private bool Execute(TurnOwner actor, BattleSide target)
        {
            var state = _session.State;
            if (_executing || state.TurnCount == 0 || state.IsFinished || state.CurrentTurn != actor) return false;
            _executing = true;
            try
            {
                var attacker = actor == TurnOwner.Player ? state.Player : state.Enemy;
                var character = _characters.Get(attacker.CharacterId);
                var skill = _skills.Get(character.DefaultSkillId);
                var damage = _rules.CalculateDamage(skill.Power);
                state.ApplyDamage(target, damage);
                var hp = target == BattleSide.Player ? state.Player.Hp : state.Enemy.Hp;
                _damage.Publish(new DamageAppliedMessage(target, damage, hp));
                if (state.IsFinished)
                    _finish.Publish(new BattleFinishedMessage(state.Winner.Value));
                else
                {
                    state.NextTurn();
                    _turn.Publish(new TurnChangedMessage(state.CurrentTurn, state.TurnCount));
                }
                return true;
            }
            finally { _executing = false; }
        }
    }
}

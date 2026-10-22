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
        private readonly IPublisher<BattleActionResolvedMessage> _action;

        public AttackUseCase(IBattleSession battleSession, BattleSystem battleSystem,
            ISkillRepository skillRepository, ICharacterRepository characterRepository,
            IPublisher<DamageAppliedMessage> damagePublisher, IPublisher<TurnChangedMessage> turnPublisher,
            IPublisher<BattleFinishedMessage> finishPublisher,
            IPublisher<BattleActionResolvedMessage> actionPublisher)
        {
            _session = battleSession; _rules = battleSystem; _skills = skillRepository;
            _characters = characterRepository; _damage = damagePublisher;
            _turn = turnPublisher; _finish = finishPublisher; _action = actionPublisher;
        }

        public bool ExecutePlayerAttack() => ExecutePlayerAction(BattleAction.Attack);
        public bool ExecutePlayerAction(BattleAction action) => Execute(TurnOwner.Player, BattleSide.Enemy, action);
        public bool ExecuteEnemyAttack() => Execute(TurnOwner.Enemy, BattleSide.Player);

        private bool Execute(TurnOwner actor, BattleSide target, BattleAction action = BattleAction.Attack)
        {
            var state = _session.State;
            if (_executing || state.TurnCount == 0 || state.IsFinished || state.CurrentTurn != actor) return false;
            if (actor == TurnOwner.Player && !state.CanPlayerChoose(action)) return false;
            _executing = true;
            try
            {
                var attacker = actor == TurnOwner.Player ? state.Player : state.Enemy;
                var character = _characters.Get(attacker.CharacterId);
                var skill = _skills.Get(character.DefaultSkillId);
                var result = actor == TurnOwner.Player
                    ? _rules.ApplyPlayerAction(state, action, skill.Power)
                    : _rules.ApplyEnemyAction(state, skill.Power);
                if (result.Action != BattleAction.Defend)
                    _damage.Publish(new DamageAppliedMessage(target, result.Damage, result.RemainingHp));
                _action.Publish(new BattleActionResolvedMessage(result));
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

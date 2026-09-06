using System.Collections.Generic;
using R3;

namespace Presentation.Battle
{
    public sealed class BattleViewModel
    {
        public ReactiveProperty<int> PlayerHp { get; } = new(0);
        public ReactiveProperty<int> PlayerMaxHp { get; } = new(0);
        public ReactiveProperty<int> EnemyHp { get; } = new(0);
        public ReactiveProperty<int> EnemyMaxHp { get; } = new(0);
        public ReactiveProperty<int> TurnCount { get; } = new(1);
        public ReactiveProperty<string> TurnText { get; } = new(string.Empty);
        public ReactiveProperty<bool> CanPlayerAct { get; } = new(true);
        public ReactiveProperty<string> LogText { get; } = new(string.Empty);

        private readonly List<string> _logs = new();

        public void AppendLog(string log)
        {
            _logs.Add(log);
            LogText.Value = string.Join("\n", _logs);
        }
    }
}
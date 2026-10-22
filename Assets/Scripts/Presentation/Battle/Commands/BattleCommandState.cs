namespace Presentation.Battle.Commands
{
    public readonly struct BattleCommandState
    {
        public bool CanExecute { get; }
        public int Cooldown { get; }
        public BattleCommandState(bool canExecute, int cooldown)
        { CanExecute = canExecute; Cooldown = cooldown; }
    }
}

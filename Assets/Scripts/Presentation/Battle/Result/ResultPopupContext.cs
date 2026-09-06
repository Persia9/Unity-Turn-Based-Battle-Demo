namespace Presentation.Battle.Result
{
    public readonly struct ResultPopupContext
    {
        public bool IsWin { get; }

        public ResultPopupContext(bool isWin)
        {
            IsWin = isWin;
        }
    }
}
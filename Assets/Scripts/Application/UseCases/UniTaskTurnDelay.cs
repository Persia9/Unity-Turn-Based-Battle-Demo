using System.Threading;
using Cysharp.Threading.Tasks;

namespace Application.UseCases
{
    public sealed class UniTaskTurnDelay : ITurnDelay
    {
        private const int DelayMilliseconds = 700;

        public UniTask Wait(CancellationToken cancellationToken)
            => UniTask.Delay(DelayMilliseconds, cancellationToken: cancellationToken);
    }
}

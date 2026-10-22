using System.Threading;
using Cysharp.Threading.Tasks;

namespace Application.UseCases
{
    public sealed class UniTaskTurnDelay : ITurnDelay
    {
        public UniTask Wait(CancellationToken cancellationToken)
            => UniTask.Delay(700, cancellationToken: cancellationToken);
    }
}

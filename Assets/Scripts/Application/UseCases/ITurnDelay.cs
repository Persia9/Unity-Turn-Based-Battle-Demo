using System.Threading;
using Cysharp.Threading.Tasks;

namespace Application.UseCases
{
    public interface ITurnDelay
    {
        UniTask Wait(CancellationToken cancellationToken);
    }
}

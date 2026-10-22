using Domain.Run;

namespace Application.Services
{
    public sealed class RunSession
    {
        public RunState State { get; } = new();
    }
}

using System;
using Domain.Run;

namespace Application.Services
{
    public interface IGameTelemetry
    {
        void TrackRunStarted();
        void TrackBattleStarted(int round, int enemyId);
        void TrackBattleFinished(int round, bool won, int turns);
        void TrackUpgradeSelected(RunUpgrade upgrade);
        void TrackRunFinished(bool cleared, int wins, int totalTurns, int totalDamageTaken);
        void RecordException(Exception exception, string context);
    }
}

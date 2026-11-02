using System;

namespace Domain.Run
{
    public static class EndlessDifficulty
    {
        public const int RoundsPerTier = 3;
        public const int BossInterval = 10;

        public static int Tier(int round)
        {
            if (round < 1) throw new ArgumentOutOfRangeException(nameof(round));
            return (round - 1) / RoundsPerTier;
        }

        public static bool IsBossRound(int round)
        {
            if (round < 1) throw new ArgumentOutOfRangeException(nameof(round));
            return round % BossInterval == 0;
        }

        public static int ScaleHp(int baseHp, int round) => Scale(baseHp, 100 + Tier(round) * 25);
        public static int ScalePower(int basePower, int round) => Scale(basePower, 100 + Tier(round) * 15);

        private static int Scale(int baseValue, int percent)
        {
            if (baseValue < 0) throw new ArgumentOutOfRangeException(nameof(baseValue));
            var value = (long)baseValue * percent / 100;
            return value >= int.MaxValue ? int.MaxValue : (int)value;
        }
    }
}

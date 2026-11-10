namespace Domain.Battle
{
    public static class BattleRules
    {
        // 防御時に受けるダメージの初期割合（%）。50の場合、元のダメージの50%を受ける。
        public const int DefaultGuardDamagePercent = 50;

        // 強攻撃を使用したターンから再使用可能になるまでのターン番号の差。
        // 3の場合、1ターン目に使用すると2・3ターン目は使用できず、4ターン目に再使用可能になる。
        public const int StrongAttackReuseTurnInterval = 3;

        // プレイヤーの強攻撃のダメージ倍率。攻撃強化分を加えた後のダメージに適用する。
        public const int PlayerStrongAttackMultiplier = 2;

        // 敵の強攻撃のダメージ倍率。プレイヤーの防御による軽減より先に適用する。
        public const int EnemyStrongAttackMultiplier = 3;

        // EveryThirdTurnパターンの敵が強攻撃を行う間隔（ターン）。3の場合、3・6・9ターン目に使用する。
        public const int EnemyStrongAttackInterval = 3;

        // BossCycleパターンの行動が一巡するターン数。
        public const int BossAttackCycleLength = 4;

        // ターン番号を行動周期で割った余りがこの値のとき、ボスが強攻撃を行う（2・6・10ターン目）。
        public const int BossStrongAttackFirstPhase = 2;

        // ターン番号を行動周期で割った余りがこの値のとき、ボスが強攻撃を行う（3・7・11ターン目）。
        public const int BossStrongAttackSecondPhase = 3;
    }
}

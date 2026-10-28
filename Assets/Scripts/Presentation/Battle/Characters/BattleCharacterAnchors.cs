using System;
using UnityEngine;

namespace Presentation.Battle.Characters
{
    public sealed class BattleCharacterAnchors
    {
        public Transform PlayerRoot { get; }
        public Transform EnemyRoot { get; }

        public BattleCharacterAnchors(Transform playerRoot, Transform enemyRoot)
        {
            PlayerRoot = playerRoot != null ? playerRoot : throw new ArgumentNullException(nameof(playerRoot));
            EnemyRoot = enemyRoot != null ? enemyRoot : throw new ArgumentNullException(nameof(enemyRoot));
        }
    }
}

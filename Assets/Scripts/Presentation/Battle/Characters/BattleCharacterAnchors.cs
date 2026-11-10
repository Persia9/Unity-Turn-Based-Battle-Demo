using System;
using UnityEngine;

namespace Presentation.Battle.Characters
{
    public sealed class BattleCharacterAnchors
    {
        public Transform PlayerRoot { get; }
        public Transform EnemyRoot { get; }
        public CubeBattleCharacterView PlayerPrefab { get; }
        public CubeBattleCharacterView EnemyPrefab { get; }
        public GameObject PlatformPrefab { get; }

        public BattleCharacterAnchors(Transform playerRoot, Transform enemyRoot,
            CubeBattleCharacterView playerPrefab, CubeBattleCharacterView enemyPrefab, GameObject platformPrefab)
        {
            PlayerRoot = playerRoot != null ? playerRoot : throw new ArgumentNullException(nameof(playerRoot));
            EnemyRoot = enemyRoot != null ? enemyRoot : throw new ArgumentNullException(nameof(enemyRoot));
            PlayerPrefab = playerPrefab != null ? playerPrefab : throw new ArgumentNullException(nameof(playerPrefab));
            EnemyPrefab = enemyPrefab != null ? enemyPrefab : throw new ArgumentNullException(nameof(enemyPrefab));
            PlatformPrefab = platformPrefab != null
                ? platformPrefab
                : throw new ArgumentNullException(nameof(platformPrefab));
        }
    }
}
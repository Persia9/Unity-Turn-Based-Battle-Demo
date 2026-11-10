using UnityEngine;
using Zenject;

namespace Presentation.Battle.Characters
{
    public sealed class JrpgBattleCameraPresenter : IInitializable
    {
        private static readonly Vector3 PlayerPosition = new(-2.6f, 0f, -1.15f);
        private static readonly Vector3 EnemyPosition = new(2.35f, 0f, 1.05f);
        private static readonly Vector3 CameraPosition = new(-6.8f, 3.75f, -8.25f);

        private readonly BattleCharacterAnchors _anchors;

        public JrpgBattleCameraPresenter(BattleCharacterAnchors anchors)
        {
            _anchors = anchors;
        }

        public void Initialize()
        {
            _anchors.PlayerRoot.localPosition = PlayerPosition;
            _anchors.EnemyRoot.localPosition = EnemyPosition;

            var battleCamera = Camera.main;
            if (battleCamera == null) return;

            battleCamera.transform.position = CameraPosition;
            battleCamera.transform.LookAt(Vector3.Lerp(PlayerPosition, EnemyPosition, 0.55f) + Vector3.up * 1.25f);
            battleCamera.fieldOfView = 46f;
            battleCamera.nearClipPlane = 0.1f;
        }
    }
}
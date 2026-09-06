using UnityEngine;

namespace Infrastructure.UI
{
    public sealed class UIRoot : MonoBehaviour
    {
        [SerializeField] private Transform _hudRoot;
        [SerializeField] private Transform _screenRoot;
        [SerializeField] private Transform _popupRoot;
        [SerializeField] private Transform _overlayRoot;
        [SerializeField] private Transform _topRoot;

        public Transform HudRoot => _hudRoot;
        public Transform ScreenRoot => _screenRoot;
        public Transform PopupRoot => _popupRoot;
        public Transform OverlayRoot => _overlayRoot;
        public Transform TopRoot => _topRoot;
    }
}
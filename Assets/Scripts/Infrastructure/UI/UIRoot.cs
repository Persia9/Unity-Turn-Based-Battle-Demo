using UnityEngine;
using UnityEngine.UI;

namespace Infrastructure.UI
{
    public sealed class UIRoot : MonoBehaviour
    {
        private CanvasScaler _scaler;
        private Canvas _scalingCanvas;
        private Vector2 _lastCanvasSize;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        private void Awake()
        {
            _scaler = GetComponentInChildren<CanvasScaler>(true);
            _scalingCanvas = _scaler.GetComponent<Canvas>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920f, 1080f);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            UpdateCanvasMatch();
            UpdateSafeArea();
        }

        private void Update()
        {
            UpdateCanvasMatch();
            UpdateSafeArea();
        }

        private void UpdateSafeArea()
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            var safeArea = Screen.safeArea;
            if (screenSize.x <= 0 || screenSize.y <= 0 ||
                (screenSize == _lastScreenSize && safeArea == _lastSafeArea)) return;
            _lastScreenSize = screenSize;
            _lastSafeArea = safeArea;
            SafeAreaUtility.Apply(_safeAreaRoot);
        }

        private void UpdateCanvasMatch()
        {
            // 16:9 より横長なら高さ、それ以外は幅に合わせる。
            var size = _scalingCanvas.pixelRect.size;
            if (size.x <= 0f || size.y <= 0f || size == _lastCanvasSize) return;
            _lastCanvasSize = size;
            _scaler.matchWidthOrHeight = size.x * 1080f > size.y * 1920f ? 1f : 0f;
        }

        [SerializeField] private RectTransform _safeAreaRoot;
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
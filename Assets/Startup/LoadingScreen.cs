using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Startup
{
    public sealed class LoadingScreen : MonoBehaviour, ILoadingScreen
    {
        private CanvasScaler _scaler;
        private Canvas _scalingCanvas;
        private Vector2 _lastCanvasSize;

        private void Awake()
        {
            _scaler = _canvas.GetComponent<CanvasScaler>();
            _scalingCanvas = _scaler.GetComponent<Canvas>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920f, 1080f);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            UpdateCanvasMatch();
        }

        private void Update() => UpdateCanvasMatch();

        private void UpdateCanvasMatch()
        {
            // 16:9 より横長なら高さ、それ以外は幅に合わせる。
            var size = _scalingCanvas.pixelRect.size;
            if (size.x <= 0f || size.y <= 0f || size == _lastCanvasSize) return;
            _lastCanvasSize = size;
            _scaler.matchWidthOrHeight = size.x * 1080f > size.y * 1920f ? 1f : 0f;
        }

        [SerializeField] private Canvas _canvas;
        [SerializeField] private TMP_Text _status;

        public void Show(string message)
        {
            _status.text = message;
            _canvas.enabled = true;
        }

        public void Hide() => _canvas.enabled = false;

        public void Fail(Exception exception)
        {
            Debug.LogException(exception);
            Show("Unable to start. Restart the game to retry.\n" + exception.Message);
        }
    }
}
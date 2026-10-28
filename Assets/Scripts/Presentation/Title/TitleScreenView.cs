using Application.UseCases;
using Cysharp.Threading.Tasks;
using Infrastructure.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Presentation.Title
{
    public sealed class TitleScreenView : UIView
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private RectTransform _title;
        [SerializeField] private RectTransform _menu;
        [Inject] private RunFlowUseCase _runFlow;
        private RectTransform _safeAreaContent;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        public override void OnOpened()
        {
            EnsureMobileLayout();
            _startButton.onClick.AddListener(OnStartClicked);
        }

        public override void OnClosed()
        {
            _startButton.onClick.RemoveListener(OnStartClicked);
        }

        private void OnStartClicked()
        {
            Run().Forget();
        }

        private async UniTask Run()
        {
            if (!_startButton.interactable) return;
            _startButton.interactable = false;
            try { await _runFlow.StartNewRun(); }
            finally { if (this != null) _startButton.interactable = true; }
        }

        private void Update()
        {
            if (_safeAreaContent == null) return;
            if (_lastSafeArea != Screen.safeArea || _lastScreenSize.x != Screen.width || _lastScreenSize.y != Screen.height)
                ApplyMobileLayout();
        }

        private void EnsureMobileLayout()
        {
            if (_safeAreaContent == null)
            {
                var safeAreaObject = new GameObject("Safe Area Content", typeof(RectTransform));
                _safeAreaContent = (RectTransform)safeAreaObject.transform;
                _safeAreaContent.SetParent(transform, false);
                _safeAreaContent.SetSiblingIndex(1);
                _title.SetParent(_safeAreaContent, false);
                _menu.SetParent(_safeAreaContent, false);
            }

            var titleText = _title.GetComponent<TMP_Text>();
            titleText.text = "CUBE BATTLE\nTHREE-BATTLE EXPEDITION";
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 24f;
            titleText.fontSizeMax = 58f;
            titleText.color = Color.white;
            titleText.raycastTarget = false;

            var startLabel = _startButton.GetComponentInChildren<TMP_Text>(true);
            if (startLabel != null)
            {
                startLabel.text = "START EXPEDITION";
                startLabel.enableAutoSizing = true;
                startLabel.fontSizeMin = 20f;
                startLabel.fontSizeMax = 36f;
                startLabel.color = Color.white;
                startLabel.raycastTarget = false;
            }

            ApplyMobileLayout();
        }

        private void ApplyMobileLayout()
        {
            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            var width = Mathf.Max(1f, Screen.width);
            var height = Mathf.Max(1f, Screen.height);
            _safeAreaContent.anchorMin = new Vector2(_lastSafeArea.xMin / width, _lastSafeArea.yMin / height);
            _safeAreaContent.anchorMax = new Vector2(_lastSafeArea.xMax / width, _lastSafeArea.yMax / height);
            _safeAreaContent.offsetMin = Vector2.zero;
            _safeAreaContent.offsetMax = Vector2.zero;

            var portrait = height > width;
            _title.anchorMin = _title.anchorMax = new Vector2(0.5f, portrait ? 0.72f : 0.7f);
            _title.anchoredPosition = Vector2.zero;
            _title.sizeDelta = new Vector2(portrait ? 620f : 820f, portrait ? 190f : 150f);

            _menu.anchorMin = _menu.anchorMax = new Vector2(0.5f, portrait ? 0.34f : 0.3f);
            _menu.anchoredPosition = Vector2.zero;
            var startRect = (RectTransform)_startButton.transform;
            startRect.sizeDelta = new Vector2(portrait ? 500f : 520f, portrait ? 120f : 108f);
        }
    }
}

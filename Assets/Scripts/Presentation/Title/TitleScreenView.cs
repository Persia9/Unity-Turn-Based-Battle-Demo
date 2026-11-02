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
        private TMP_Text _tagline;
        private TMP_Text _progress;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private bool _visualStyleConfigured;

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
            titleText.text = "<size=22><color=#75AFFF>TACTICAL TURN-BASED RPG</color></size>\n" +
                             "<size=62><b>CUBE BATTLE</b></size>\n" +
                             "<size=28><color=#B9C8DC>THREE-BATTLE EXPEDITION</color></size>";
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 24f;
            titleText.fontSizeMax = 62f;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.lineSpacing = -4f;
            titleText.raycastTarget = false;

            var startLabel = _startButton.GetComponentInChildren<TMP_Text>(true);
            if (startLabel != null)
            {
                startLabel.text = "BEGIN EXPEDITION";
                startLabel.enableAutoSizing = true;
                startLabel.fontSizeMin = 20f;
                startLabel.fontSizeMax = 36f;
                startLabel.color = Color.white;
                startLabel.raycastTarget = false;
            }

            ConfigureVisualStyle(titleText);

            ApplyMobileLayout();
        }

        private void ConfigureVisualStyle(TMP_Text titleText)
        {
            if (_visualStyleConfigured) return;
            _visualStyleConfigured = true;

            _tagline = CreateText("Expedition Rules", titleText,
                "READ THE ENEMY   •   CHOOSE YOUR ACTION   •   SURVIVE", 22f,
                new Color(0.72f, 0.8f, 0.9f));
            _progress = CreateText("Battle Progress", titleText,
                "01    •    02    •    03", 25f, new Color(1f, 0.82f, 0.42f));
            _progress.fontStyle = FontStyles.Bold;
            _progress.characterSpacing = 8f;

            CreateDecoration("Player Accent", new Color(0.08f, 0.32f, 0.72f, 0.24f),
                new Vector2(0f, 0.5f), new Vector2(-80f, 30f), new Vector2(340f, 620f), -14f);
            CreateDecoration("Enemy Accent", new Color(0.75f, 0.18f, 0.1f, 0.2f),
                new Vector2(1f, 0.5f), new Vector2(80f, -15f), new Vector2(340f, 620f), -14f);
            CreateDecoration("Center Glow", new Color(0.08f, 0.25f, 0.48f, 0.16f),
                new Vector2(0.5f, 0.54f), Vector2.zero, new Vector2(560f, 560f), 45f);

            if (_startButton.TryGetComponent<Image>(out var buttonImage))
                buttonImage.color = new Color(0.07f, 0.32f, 0.75f, 1f);
            var colors = _startButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.76f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.35f, 0.4f, 0.5f, 0.65f);
            _startButton.colors = colors;

            var label = _startButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontStyle = FontStyles.Bold;
                label.characterSpacing = 2f;
                label.outlineColor = new Color(0f, 0f, 0f, 0.7f);
                label.outlineWidth = 0.12f;
            }
        }

        private TMP_Text CreateText(string objectName, TMP_Text styleSource, string content, float size, Color color)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(_safeAreaContent, false);
            text.font = styleSource.font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private void CreateDecoration(string objectName, Color color, Vector2 anchor, Vector2 position,
            Vector2 size, float rotation)
        {
            var decoration = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)decoration.transform;
            rect.SetParent(transform, false);
            rect.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1));
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var image = decoration.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private void ApplyMobileLayout()
        {
            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            var width = Mathf.Max(1f, Screen.width);
            var height = Mathf.Max(1f, Screen.height);
            SafeAreaUtility.Apply(_safeAreaContent);

            var portrait = height > width;
            _title.anchorMin = _title.anchorMax = new Vector2(0.5f, portrait ? 0.73f : 0.72f);
            _title.anchoredPosition = Vector2.zero;
            _title.sizeDelta = new Vector2(portrait ? 650f : 920f, portrait ? 220f : 210f);

            LayoutText(_tagline, new Vector2(0.5f, portrait ? 0.49f : 0.47f),
                new Vector2(portrait ? 620f : 980f, 48f));
            LayoutText(_progress, new Vector2(0.5f, portrait ? 0.41f : 0.39f),
                new Vector2(520f, 48f));

            _menu.anchorMin = _menu.anchorMax = new Vector2(0.5f, portrait ? 0.27f : 0.23f);
            _menu.anchoredPosition = Vector2.zero;
            var startRect = (RectTransform)_startButton.transform;
            startRect.sizeDelta = new Vector2(portrait ? 520f : 560f, portrait ? 120f : 112f);
        }

        private static void LayoutText(TMP_Text text, Vector2 anchor, Vector2 size)
        {
            if (text == null) return;
            var rect = (RectTransform)text.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }
    }
}

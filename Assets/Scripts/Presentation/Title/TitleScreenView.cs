using Application.UseCases;
using Cysharp.Threading.Tasks;
using Infrastructure.UI;
using Application.Services;
using Presentation.Localization;
using Domain.Run;
using Infrastructure.Localization;
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
        [Inject] private ILocalizationService _localization;
        private RectTransform _safeAreaContent;
        private TMP_Text _tagline;
        private TMP_Text _progress;
        private Button _languageButton;
        private Button _endlessButton;
        private TMP_Text _languageLabel;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private bool _visualStyleConfigured;

        public override void OnOpened()
        {
            EnsureMobileLayout();
            _startButton.onClick.AddListener(OnExpeditionClicked);
            _endlessButton.onClick.AddListener(OnEndlessClicked);
            _languageButton.onClick.AddListener(OnLanguageClicked);
            _localization.LanguageChanged += RefreshLocalization;
            RefreshLocalization();
        }

        public override void OnClosed()
        {
            _startButton.onClick.RemoveListener(OnExpeditionClicked);
            if (_endlessButton != null) _endlessButton.onClick.RemoveListener(OnEndlessClicked);
            if (_languageButton != null) _languageButton.onClick.RemoveListener(OnLanguageClicked);
            _localization.LanguageChanged -= RefreshLocalization;
        }

        private void OnExpeditionClicked() => Run(RunMode.Expedition).Forget();
        private void OnEndlessClicked() => Run(RunMode.Endless).Forget();

        private async UniTask Run(RunMode mode)
        {
            if (!_startButton.interactable || !_endlessButton.interactable) return;
            _startButton.interactable = false;
            _endlessButton.interactable = false;
            try
            {
                await _runFlow.StartNewRun(mode);
            }
            finally
            {
                if (this != null)
                {
                    _startButton.interactable = true;
                    _endlessButton.interactable = true;
                }
            }
        }

        private void Update()
        {
            if (_safeAreaContent == null) return;
            if (_lastSafeArea != Screen.safeArea || _lastScreenSize.x != Screen.width ||
                _lastScreenSize.y != Screen.height)
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

            // The original prefab used a single-button vertical layout. Mode selection is
            // positioned explicitly so landscape can use two columns without being overridden.
            if (_menu.TryGetComponent<LayoutGroup>(out var layoutGroup)) layoutGroup.enabled = false;
            if (_menu.TryGetComponent<ContentSizeFitter>(out var sizeFitter)) sizeFitter.enabled = false;

            _tagline = CreateText("Expedition Rules", titleText,
                "READ THE ENEMY   •   CHOOSE YOUR ACTION   •   SURVIVE", 22f,
                new Color(0.72f, 0.8f, 0.9f));
            _progress = CreateText("Battle Progress", titleText,
                "01    •    02    •    03", 25f, new Color(1f, 0.82f, 0.42f));
            _progress.fontStyle = FontStyles.Bold;
            _progress.characterSpacing = 8f;
            CreateLanguageButton(titleText);

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

            _endlessButton = Instantiate(_startButton, _menu);
            _endlessButton.name = "Endless Mode Button";
            if (_endlessButton.TryGetComponent<Image>(out var endlessImage))
                endlessImage.color = new Color(0.68f, 0.22f, 0.08f, 1f);

            var label = _startButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontStyle = FontStyles.Bold;
                label.characterSpacing = 2f;
                label.outlineColor = new Color(0f, 0f, 0f, 0.7f);
                label.outlineWidth = 0.12f;
            }

            var endlessLabel = _endlessButton.GetComponentInChildren<TMP_Text>(true);
            if (endlessLabel != null)
            {
                endlessLabel.fontStyle = FontStyles.Bold;
                endlessLabel.characterSpacing = 2f;
                endlessLabel.outlineColor = new Color(0f, 0f, 0f, 0.7f);
                endlessLabel.outlineWidth = 0.12f;
            }
        }

        private void CreateLanguageButton(TMP_Text styleSource)
        {
            var buttonObject = new GameObject("Language Button", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            var rect = (RectTransform)buttonObject.transform;
            rect.SetParent(_safeAreaContent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-18f, -18f);
            rect.sizeDelta = new Vector2(108f, 54f);
            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.09f, 0.14f, 0.22f, 0.95f);
            _languageButton = buttonObject.GetComponent<Button>();
            _languageButton.targetGraphic = image;

            _languageLabel = CreateText("Language Label", styleSource, string.Empty, 20f, Color.white);
            _languageLabel.transform.SetParent(rect, false);
            var labelRect = (RectTransform)_languageLabel.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            _languageLabel.fontStyle = FontStyles.Bold;
        }

        private void OnLanguageClicked() => _localization.ToggleLanguage();

        private void RefreshLocalization()
        {
            if (_title == null || _localization == null) return;
            var titleText = _title.GetComponent<TMP_Text>();
            titleText.text = $"<size=22><color=#75AFFF>{_localization.Get("title.genre")}</color></size>\n" +
                             "<size=62><b>CUBE BATTLE</b></size>\n" +
                             $"<size=28><color=#B9C8DC>{_localization.Get("title.subtitle")}</color></size>";
            if (_tagline != null) _tagline.text = _localization.Get("title.tagline");
            if (_progress != null) _progress.text = "01    •    02    •    03";
            var startLabel = _startButton.GetComponentInChildren<TMP_Text>(true);
            if (startLabel != null) startLabel.text = _localization.Get("title.start.expedition");
            var endlessLabel = _endlessButton != null ? _endlessButton.GetComponentInChildren<TMP_Text>(true) : null;
            if (endlessLabel != null) endlessLabel.text = _localization.Get("title.start.endless");
            if (_languageLabel != null) _languageLabel.text = _localization.Get("language.toggle");
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                LocalizedFontProvider.Apply(text, _localization);
        }

        private TMP_Text CreateText(string objectName, TMP_Text styleSource, string content, float size, Color color)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
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

            LayoutText(_tagline, new Vector2(0.5f, portrait ? 0.5f : 0.48f),
                new Vector2(portrait ? 620f : 980f, 48f));
            LayoutText(_progress, new Vector2(0.5f, portrait ? 0.43f : 0.37f),
                new Vector2(520f, 48f));

            _menu.anchorMin = _menu.anchorMax = new Vector2(0.5f, portrait ? 0.25f : 0.2f);
            _menu.anchoredPosition = Vector2.zero;
            _menu.sizeDelta = portrait ? new Vector2(560f, 252f) : new Vector2(1160f, 120f);
            var startRect = (RectTransform)_startButton.transform;
            var endlessRect = (RectTransform)_endlessButton.transform;
            startRect.anchorMin = startRect.anchorMax = new Vector2(0.5f, 0.5f);
            endlessRect.anchorMin = endlessRect.anchorMax = new Vector2(0.5f, 0.5f);
            if (portrait)
            {
                startRect.anchoredPosition = new Vector2(0f, 64f);
                endlessRect.anchoredPosition = new Vector2(0f, -64f);
                startRect.sizeDelta = endlessRect.sizeDelta = new Vector2(520f, 104f);
            }
            else
            {
                startRect.anchoredPosition = new Vector2(-290f, 0f);
                endlessRect.anchoredPosition = new Vector2(290f, 0f);
                startRect.sizeDelta = endlessRect.sizeDelta = new Vector2(540f, 104f);
            }
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
using System;
using Application.Services;
using Application.UseCases;
using Cysharp.Threading.Tasks;
using Domain.Run;
using Infrastructure.Localization;
using Infrastructure.UI;
using Presentation.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Zenject;

namespace Presentation.Battle.Result
{
    public sealed class ResultPopupView : UIView, IUIContextReceiver<ResultPopupContext>
    {
        [Serializable]
        private sealed class UpgradeSlot
        {
            public Button Button;
            public RunUpgrade Upgrade;
            [NonSerialized] public UnityAction Handler;
        }
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private UpgradeSlot[] _upgrades;
        [Inject] private ReturnToTitleUseCase _returnToTitle;
        [Inject] private RunFlowUseCase _flow;
        [Inject] private RunSession _run;
        [Inject] private ILocalizationService _localization;
        private ResultPopupContext _context;
        private bool _upgradeScreen;
        private RunUpgrade? _selectedUpgrade;
        private string _summary;
        private RectTransform _root;
        private RectTransform _panel;
        private TMP_Text _titleText;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private bool _visualStyleConfigured;
        public void SetContext(ResultPopupContext context) => _context = context;
        public override void OnOpened()
        {
            ConfigureMobileLayout();
            _selectedUpgrade = null;
            var state = _run.State;
            _upgradeScreen = state.Phase == RunPhase.ChoosingUpgrade;
            if (_titleText != null)
                _titleText.text = _localization.Get(state.Mode == RunMode.Endless ? "result.title.endless" : "result.title");
            var roundClearKey = state.Mode == RunMode.Endless ? "result.round_clear.endless" : "result.round_clear";
            var heading = _upgradeScreen ? _localization.Get(roundClearKey, state.Round)
                : state.Phase == RunPhase.Cleared && _context.IsWin
                    ? _localization.Get("result.cleared")
                    : _localization.Get(state.Mode == RunMode.Endless ? "result.failed.endless" : "result.failed");
            _summary = BuildSummary(heading, state);
            _resultText.text = _summary;
            _closeButton.onClick.AddListener(OnTitleClicked);
            _retryButton.gameObject.SetActive(!_upgradeScreen);
            _retryButton.onClick.AddListener(OnRetryClicked);
            foreach (var slot in _upgrades)
            {
                var selected = slot;
                slot.Button.gameObject.SetActive(_upgradeScreen);
                slot.Handler = () => Continue(selected.Upgrade).Forget();
                slot.Button.onClick.AddListener(slot.Handler);
            }
            RefreshButtonLabels();
            SetBusy(false);
        }

        private void Update()
        {
            if (_root == null) return;
            if (_lastSafeArea != Screen.safeArea || _lastScreenSize.x != Screen.width || _lastScreenSize.y != Screen.height)
                ApplyMobileLayout();
        }

        private void ConfigureMobileLayout()
        {
            _root = (RectTransform)transform;
            _panel = transform.Find("background") as RectTransform;
            _titleText = _panel != null ? _panel.Find("titleText")?.GetComponent<TMP_Text>() : null;
            ConfigureText(_resultText, 17f, 34f);
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                var label = button.GetComponentInChildren<TMP_Text>(true);
                ConfigureText(label, 16f, 28f);
                var rect = (RectTransform)button.transform;
                if (rect.sizeDelta.y < 84f) rect.sizeDelta = new Vector2(rect.sizeDelta.x, 84f);
            }
            ConfigureVisualStyle();
            ApplyMobileLayout();
        }

        private string BuildSummary(string heading, RunState state)
        {
            var guidanceKey = _upgradeScreen ? "result.guidance.upgrade"
                : state.Mode == RunMode.Endless ? "result.guidance.retry.endless" : "result.guidance.retry";
            var guidance = _localization.Get(guidanceKey);
            var winsKey = state.Mode == RunMode.Endless ? "result.wins.endless" : "result.wins";
            var reachedKey = state.Mode == RunMode.Endless ? "result.reached.endless" : "result.reached";
            return $"<size=34><b><color=#FFD36A>{heading.ToUpperInvariant()}</color></b></size>\n\n" +
                   $"<size=23><color=#9FB3CA>{_localization.Get("result.progress")}</color>   " +
                   $"<b>{_localization.Get(winsKey, state.Wins)}</b>   •   {_localization.Get(reachedKey, state.Round)}</size>\n" +
                   $"<size=21>{_localization.Get("result.turns")}  <b>{state.TotalTurns}</b>      " +
                   $"{_localization.Get("result.damage")}  <b>{state.TotalDamageTaken}</b></size>\n" +
                   $"<size=22><color=#77B7FF>HP  <b>{state.Hp} / {state.MaxHp}</b></color>      " +
                   $"<color=#FF987F>ATK  <b>+{state.DamageBonus}</b></color>      " +
                   $"<color=#80DDD2>GUARD  <b>{100 - state.GuardDamagePercent}%</b></color></size>\n" +
                   $"<size=19><color=#B8C5D6>{guidance}</color></size>";
        }

        private void ConfigureVisualStyle()
        {
            if (_visualStyleConfigured || _panel == null) return;
            _visualStyleConfigured = true;

            if (_panel.TryGetComponent<Image>(out var panelImage))
                panelImage.color = new Color(0.025f, 0.045f, 0.075f, 0.97f);
            var mask = transform.Find("mask");
            if (mask != null && mask.TryGetComponent<Image>(out var maskImage))
                maskImage.color = new Color(0.01f, 0.02f, 0.04f, 0.62f);

            if (_titleText != null)
            {
                _titleText.text = _localization.Get("result.title");
                _titleText.color = new Color(0.76f, 0.84f, 0.96f);
                _titleText.fontStyle = FontStyles.Bold;
                _titleText.characterSpacing = 4f;
                _titleText.raycastTarget = false;
            }
            _resultText.color = Color.white;
            _resultText.fontStyle = FontStyles.Normal;
            _resultText.alignment = TextAlignmentOptions.Top;
            _resultText.lineSpacing = 8f;
            _resultText.outlineColor = new Color(0f, 0f, 0f, 0.65f);
            _resultText.outlineWidth = 0.1f;
            LocalizedFontProvider.Apply(_titleText, _localization);
            LocalizedFontProvider.Apply(_resultText, _localization);

            var upgradeColors = new[]
            {
                new Color(0.08f, 0.3f, 0.62f, 0.98f),
                new Color(0.05f, 0.42f, 0.36f, 0.98f),
                new Color(0.62f, 0.3f, 0.07f, 0.98f)
            };
            for (var index = 0; index < _upgrades.Length; index++)
                StyleButton(_upgrades[index].Button, upgradeColors[Mathf.Min(index, upgradeColors.Length - 1)], true);
            StyleButton(_retryButton, new Color(0.08f, 0.3f, 0.62f, 0.98f), true);
            StyleButton(_closeButton, new Color(0.1f, 0.14f, 0.2f, 0.98f), false);
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                LocalizedFontProvider.Apply(text, _localization);
        }

        private void RefreshButtonLabels()
        {
            var keys = new[] { "result.attack", "result.vitality", "result.guard" };
            for (var index = 0; index < _upgrades.Length; index++)
            {
                var label = _upgrades[index].Button.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = _localization.Get(keys[Mathf.Min(index, keys.Length - 1)]);
            }
            var retryLabel = _retryButton.GetComponentInChildren<TMP_Text>(true);
            if (retryLabel != null) retryLabel.text = _localization.Get(
                _run.State.Mode == RunMode.Endless ? "result.retry.endless" : "result.retry");
            var closeLabel = _closeButton.GetComponentInChildren<TMP_Text>(true);
            if (closeLabel != null) closeLabel.text = _localization.Get("result.return");
        }

        private void ApplyMobileLayout()
        {
            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            SafeAreaUtility.Apply(_root);
            if (_panel == null) return;
            _panel.anchorMin = new Vector2(0.07f, 0.07f);
            _panel.anchorMax = new Vector2(0.93f, 0.93f);
            _panel.offsetMin = Vector2.zero;
            _panel.offsetMax = Vector2.zero;

            if (_titleText != null)
            {
                var titleRect = (RectTransform)_titleText.transform;
                titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0f, -24f);
                titleRect.sizeDelta = new Vector2(900f, 54f);
            }
            var resultRect = (RectTransform)_resultText.transform;
            resultRect.anchorMin = resultRect.anchorMax = new Vector2(0.5f, 1f);
            resultRect.pivot = new Vector2(0.5f, 1f);
            resultRect.anchoredPosition = new Vector2(0f, -104f);
            resultRect.sizeDelta = new Vector2(Mathf.Min(1100f, _panel.rect.width - 80f), 280f);

            var cardWidth = Mathf.Clamp((_panel.rect.width - 100f) / 3f, 250f, 340f);
            for (var index = 0; index < _upgrades.Length; index++)
            {
                var rect = (RectTransform)_upgrades[index].Button.transform;
                rect.anchorMin = rect.anchorMax = new Vector2((index + 0.5f) / _upgrades.Length, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 104f);
                rect.sizeDelta = new Vector2(cardWidth, 138f);
            }
            LayoutBottomButton(_retryButton, 104f, 360f, 110f);
            LayoutBottomButton(_closeButton, 18f, 260f, 70f);
        }

        private static void LayoutBottomButton(Button button, float y, float width, float height)
        {
            if (button == null) return;
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void StyleButton(Button button, Color baseColor, bool emphasize)
        {
            if (button == null) return;
            if (button.TryGetComponent<Image>(out var image)) image.color = baseColor;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.35f, 0.38f, 0.42f, 0.62f);
            button.colors = colors;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;
            label.color = Color.white;
            label.fontStyle = emphasize ? FontStyles.Bold : FontStyles.Normal;
            label.outlineColor = new Color(0f, 0f, 0f, 0.65f);
            label.outlineWidth = 0.12f;
        }

        private static void ConfigureText(TMP_Text text, float minimum, float maximum)
        {
            if (text == null) return;
            text.enableAutoSizing = true;
            text.fontSizeMin = minimum;
            text.fontSizeMax = maximum;
            text.raycastTarget = false;
        }
        public override void OnClosed()
        {
            _closeButton.onClick.RemoveListener(OnTitleClicked);
            _retryButton.onClick.RemoveListener(OnRetryClicked);
            foreach (var slot in _upgrades)
                if (slot.Handler != null) { slot.Button.onClick.RemoveListener(slot.Handler); slot.Handler = null; }
        }
        private void SetBusy(bool busy)
        {
            _closeButton.interactable = !busy;
            _retryButton.interactable = !busy;
            foreach (var slot in _upgrades)
                slot.Button.interactable = !busy && (!_selectedUpgrade.HasValue || _selectedUpgrade == slot.Upgrade);
        }
        private async UniTask Continue(RunUpgrade upgrade)
        {
            if (_selectedUpgrade.HasValue && _selectedUpgrade != upgrade) return;
            _selectedUpgrade = upgrade; SetBusy(true);
            try { await _flow.ChooseUpgrade(upgrade); }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (this != null) _resultText.text = _summary + "\n" + _localization.Get("result.load_upgrade_failed");
            }
            finally { if (this != null) SetBusy(false); }
        }
        private void OnRetryClicked() => Retry().Forget();
        private async UniTask Retry()
        {
            SetBusy(true);
            try { await _flow.StartNewRun(_run.State.Mode); }
            catch (Exception error) { Debug.LogException(error); if (this != null) _resultText.text = _summary + "\n" + _localization.Get("result.load_retry_failed"); }
            finally { if (this != null) SetBusy(false); }
        }
        private void OnTitleClicked() => ReturnToTitle().Forget();
        private async UniTask ReturnToTitle()
        {
            SetBusy(true);
            try { await _returnToTitle.Execute(); }
            catch (Exception error) { Debug.LogException(error); }
            finally { if (this != null) SetBusy(false); }
        }
    }
}

using Application.Services;
using Application.UseCases;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Domain.Battle;
using Infrastructure.UI;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Presentation.Battle.Commands;
using Zenject;

namespace Presentation.Battle
{
    public sealed class BattleHudView : UIView
    {
        [SerializeField] private TMP_Text _enemyIntentText;
        [SerializeField] private TMP_Text _defenseText;
        [SerializeField] private TMP_Text _turnText;
        [SerializeField] private TMP_Text _runText;
        [SerializeField] private TMP_Text _playerHpText;
        [SerializeField] private TMP_Text _enemyHpText;
        [SerializeField] private TMP_Text _logText;

        [Inject] private BattleViewModel _viewModel;
        [Inject] private PlayPlayerTurnUseCase _playTurn;
        [Inject] private IBattleSession _battleSession;
        [Inject] private RunSession _runSession;

        private BattleActionButton[] _actionButtons;
        private CompositeDisposable _disposables;
        private System.Threading.CancellationTokenSource _turnCancellation;
        private RectTransform _root;
        private ScrollRect _logScroll;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private readonly List<PanelBinding> _panels = new();
        private bool _visualStyleConfigured;

        private readonly struct PanelBinding
        {
            public RectTransform Panel { get; }
            public RectTransform Target { get; }
            public Vector2 Padding { get; }

            public PanelBinding(RectTransform panel, RectTransform target, Vector2 padding)
            {
                Panel = panel;
                Target = target;
                Padding = padding;
            }
        }

        public override void OnOpened()
        {
            _disposables = new CompositeDisposable();
            _turnCancellation = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            _actionButtons = GetComponentsInChildren<BattleActionButton>(true);
            foreach (var button in _actionButtons) button.Bind(_viewModel, ExecuteAction);
            ConfigureMobileLayout();

            _viewModel.TurnText.Subscribe(x => _turnText.text = FormatTurnText(x)).AddTo(_disposables);
            _viewModel.PlayerHp.Subscribe(_ => RefreshPlayerHp()).AddTo(_disposables);
            _viewModel.PlayerMaxHp.Subscribe(_ => RefreshPlayerHp()).AddTo(_disposables);
            _viewModel.EnemyHp.Subscribe(_ => RefreshEnemyHp()).AddTo(_disposables);
            _viewModel.EnemyMaxHp.Subscribe(_ => RefreshEnemyHp()).AddTo(_disposables);
            _viewModel.LogText.Subscribe(x => _logText.text = x).AddTo(_disposables);
            _viewModel.EnemyIntentText.Subscribe(x => _enemyIntentText.text = x).AddTo(_disposables);
            _viewModel.DefenseText.Subscribe(x => _defenseText.text = x).AddTo(_disposables);

            var state = _battleSession.State;
            var run = _runSession.State;
            _runText.text = $"BATTLE {run.Round:00} / 03   {state.Enemy.Name.ToUpperInvariant()}\n" +
                            $"ATK +{run.DamageBonus}    GUARD {100 - run.GuardDamagePercent}%";
            _viewModel.PlayerMaxHp.Value = state.Player.MaxHp;
            _viewModel.PlayerHp.Value = state.Player.Hp;
            _viewModel.EnemyMaxHp.Value = state.Enemy.MaxHp;
            _viewModel.EnemyHp.Value = state.Enemy.Hp;
            _viewModel.TurnCount.Value = state.TurnCount;
            _viewModel.CanPlayerAct.Value = !state.IsFinished && state.CurrentTurn == TurnOwner.Player;
            _viewModel.TurnText.Value = $"Turn {state.TurnCount} / {state.CurrentTurn}";
            _viewModel.RefreshCommands(state);
            _viewModel.AppendLog("Battle Start: choose an action.");
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
            _logScroll = GetComponentInChildren<ScrollRect>(true);
            ReparentToHudRoot(_turnText);
            ReparentToHudRoot(_playerHpText);
            ReparentToHudRoot(_enemyHpText);
            ConfigureText(_enemyIntentText, 18f, 28f);
            ConfigureText(_defenseText, 16f, 24f);
            ConfigureText(_turnText, 18f, 30f);
            ConfigureText(_runText, 18f, 28f);
            ConfigureText(_playerHpText, 18f, 30f);
            ConfigureText(_enemyHpText, 18f, 30f);
            ConfigureText(_logText, 16f, 24f);
            ConfigureVisualStyle();
            ApplyMobileLayout();
        }

        private void ConfigureVisualStyle()
        {
            if (_visualStyleConfigured) return;
            _visualStyleConfigured = true;

            var primaryPanel = new Color(0.035f, 0.055f, 0.095f, 0.9f);
            var secondaryPanel = new Color(0.045f, 0.075f, 0.12f, 0.86f);
            CreatePanelBehind(_runText, "Run Status Panel", primaryPanel, new Vector2(18f, 10f));
            CreatePanelBehind(_turnText, "Turn Panel", new Color(0.07f, 0.12f, 0.2f, 0.92f), new Vector2(22f, 10f));
            CreatePanelBehind(_playerHpText, "Player Status Panel", new Color(0.035f, 0.12f, 0.23f, 0.9f), new Vector2(18f, 12f));
            CreatePanelBehind(_enemyHpText, "Enemy Status Panel", new Color(0.24f, 0.055f, 0.055f, 0.9f), new Vector2(18f, 12f));
            CreatePanelBehind(_enemyIntentText, "Enemy Intent Panel", secondaryPanel, new Vector2(18f, 8f));
            CreatePanelBehind(_defenseText, "Defense Panel", secondaryPanel, new Vector2(18f, 6f));

            SetTextStyle(_runText, TextAlignmentOptions.Left, new Color(0.9f, 0.94f, 1f));
            SetTextStyle(_turnText, TextAlignmentOptions.Center, Color.white);
            SetTextStyle(_playerHpText, TextAlignmentOptions.Left, Color.white);
            SetTextStyle(_enemyHpText, TextAlignmentOptions.Right, Color.white);
            SetTextStyle(_enemyIntentText, TextAlignmentOptions.Right, new Color(1f, 0.86f, 0.48f));
            SetTextStyle(_defenseText, TextAlignmentOptions.Right, new Color(0.72f, 0.84f, 1f));
            SetTextStyle(_logText, TextAlignmentOptions.TopLeft, new Color(0.92f, 0.95f, 1f));

            if (_logScroll != null && _logScroll.TryGetComponent<Image>(out var logBackground))
                logBackground.color = new Color(0.025f, 0.04f, 0.07f, 0.86f);
        }

        private void ApplyMobileLayout()
        {
            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            SafeAreaUtility.Apply(_root);
            Canvas.ForceUpdateCanvases();

            var availableWidth = Mathf.Max(720f, _root.rect.width);
            var buttonWidth = Mathf.Clamp((availableWidth - 72f) / 3f, 170f, 320f);
            if (_actionButtons != null)
            {
                System.Array.Sort(_actionButtons, (left, right) =>
                    ((RectTransform)left.transform).anchoredPosition.x.CompareTo(((RectTransform)right.transform).anchoredPosition.x));
                for (var index = 0; index < _actionButtons.Length; index++)
                {
                    var rect = (RectTransform)_actionButtons[index].transform;
                    rect.anchorMin = rect.anchorMax = new Vector2((index + 0.5f) / _actionButtons.Length, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = new Vector2(0f, 24f);
                    rect.sizeDelta = new Vector2(buttonWidth, 118f);
                    ConfigureButton(_actionButtons[index], index);
                }
            }

            if (_logScroll != null)
            {
                var logRect = (RectTransform)_logScroll.transform;
                logRect.anchorMin = new Vector2(0f, 0f);
                logRect.anchorMax = new Vector2(0.56f, 0f);
                logRect.pivot = new Vector2(0f, 0f);
                logRect.anchoredPosition = new Vector2(28f, 164f);
                logRect.sizeDelta = new Vector2(-42f, 138f);
            }

            LayoutText(_runText, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -24f), new Vector2(470f, 70f));
            LayoutText(_turnText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(390f, 54f));
            LayoutText(_playerHpText, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -122f), new Vector2(310f, 70f));
            LayoutText(_enemyHpText, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -104f), new Vector2(310f, 70f));
            LayoutText(_enemyIntentText, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -196f), new Vector2(500f, 52f));
            LayoutText(_defenseText, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -264f), new Vector2(500f, 40f));
            SyncPanels();
        }

        private void ReparentToHudRoot(TMP_Text text)
        {
            if (text == null || text.transform.parent == _root) return;
            text.transform.SetParent(_root, false);
        }

        private void CreatePanelBehind(TMP_Text text, string panelName, Color color, Vector2 padding)
        {
            if (text == null) return;
            var target = (RectTransform)text.transform;
            var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var panel = (RectTransform)panelObject.transform;
            panel.SetParent(target.parent, false);
            panel.SetSiblingIndex(target.GetSiblingIndex());
            var image = panelObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            _panels.Add(new PanelBinding(panel, target, padding));
        }

        private static void SetTextStyle(TMP_Text text, TextAlignmentOptions alignment, Color color)
        {
            if (text == null) return;
            text.alignment = alignment;
            text.color = color;
            text.outlineColor = new Color(0f, 0f, 0f, 0.65f);
            text.outlineWidth = 0.12f;
        }

        private static void LayoutText(TMP_Text text, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            if (text == null) return;
            var rect = (RectTransform)text.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void SyncPanels()
        {
            foreach (var binding in _panels)
            {
                if (binding.Panel == null || binding.Target == null) continue;
                binding.Panel.anchorMin = binding.Target.anchorMin;
                binding.Panel.anchorMax = binding.Target.anchorMax;
                binding.Panel.pivot = binding.Target.pivot;
                binding.Panel.anchoredPosition = binding.Target.anchoredPosition;
                binding.Panel.sizeDelta = binding.Target.sizeDelta + binding.Padding * 2f;
            }
        }

        private static void ConfigureButton(BattleActionButton actionButton, int index)
        {
            if (actionButton == null || !actionButton.TryGetComponent<Image>(out var image)) return;
            var normal = index switch
            {
                0 => new Color(0.08f, 0.3f, 0.62f, 0.96f),
                1 => new Color(0.05f, 0.42f, 0.46f, 0.96f),
                _ => new Color(0.62f, 0.25f, 0.08f, 0.96f)
            };
            image.color = normal;
            var button = actionButton.GetComponent<Button>();
            if (button != null)
            {
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
                colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(0.35f, 0.35f, 0.38f, 0.65f);
                button.colors = colors;
            }
            var label = actionButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = Color.white;
                label.fontStyle = FontStyles.Bold;
                label.outlineColor = new Color(0f, 0f, 0f, 0.7f);
                label.outlineWidth = 0.15f;
            }
        }

        private static string FormatTurnText(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace("Turn", "TURN").Replace(" / ", "  •  ").ToUpperInvariant();
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
            if (_actionButtons != null)
                foreach (var button in _actionButtons) if (button != null) button.Unbind();
            _disposables?.Dispose();
            _turnCancellation?.Cancel();
            _turnCancellation?.Dispose();
            _turnCancellation = null;
        }

        private void RefreshPlayerHp()
        {
            _playerHpText.text = $"<size=18><color=#82B7FF>HERO</color></size>\nHP  {_viewModel.PlayerHp.Value} / {_viewModel.PlayerMaxHp.Value}";
        }

        private void RefreshEnemyHp()
        {
            _enemyHpText.text = $"<size=18><color=#FF9B8E>ENEMY</color></size>\nHP  {_viewModel.EnemyHp.Value} / {_viewModel.EnemyMaxHp.Value}";
        }

        private void ExecuteAction(BattleAction action)
        {
            if (_turnCancellation != null)
                _playTurn.Execute(action, _turnCancellation.Token).Forget();
        }

        private void OnDestroy() => OnClosed();
    }
}

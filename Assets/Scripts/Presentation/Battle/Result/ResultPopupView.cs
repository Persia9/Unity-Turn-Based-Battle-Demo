using System;
using Application.Services;
using Application.UseCases;
using Cysharp.Threading.Tasks;
using Domain.Run;
using Infrastructure.UI;
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
        private ResultPopupContext _context;
        private bool _upgradeScreen;
        private RunUpgrade? _selectedUpgrade;
        private string _summary;
        public void SetContext(ResultPopupContext context) => _context = context;
        public override void OnOpened()
        {
            _selectedUpgrade = null;
            var state = _run.State;
            _upgradeScreen = state.Phase == RunPhase.ChoosingUpgrade;
            var heading = _upgradeScreen ? $"Battle {state.Round}/3 cleared!"
                : state.Phase == RunPhase.Cleared && _context.IsWin ? "EXPEDITION CLEARED!" : "EXPEDITION FAILED";
            _summary = $"{heading}\nWins: {state.Wins}/3 | Reached: {state.Round}/3\n" +
                $"Total turns: {state.TotalTurns} | Damage taken: {state.TotalDamageTaken}\n" +
                $"HP: {state.Hp}/{state.MaxHp} | Attack bonus: +{state.DamageBonus} | Guard reduction: {100 - state.GuardDamagePercent}%\n" +
                (_upgradeScreen ? "Recovered up to 20 HP. Choose ONE upgrade for the next battle." : "Retry starts a fresh expedition from battle 1.");
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
            SetBusy(false);
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
                if (this != null) _resultText.text = _summary + "\nLoad failed. Press your chosen upgrade again to retry.";
            }
            finally { if (this != null) SetBusy(false); }
        }
        private void OnRetryClicked() => Retry().Forget();
        private async UniTask Retry()
        {
            SetBusy(true);
            try { await _flow.StartNewRun(); }
            catch (Exception error) { Debug.LogException(error); if (this != null) _resultText.text = _summary + "\nLoad failed. Retry or return to title."; }
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

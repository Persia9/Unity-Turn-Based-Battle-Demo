using System;
using Domain.Battle;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Presentation.Battle.Commands
{
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class BattleActionButton : MonoBehaviour
    {
        [SerializeField] private BattleActionDefinition _definition;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _description;
        private Button _button;
        private BattleViewModel _viewModel;
        private Action<BattleAction> _execute;
        private IDisposable _subscription;

        public void Bind(BattleViewModel viewModel, Action<BattleAction> execute)
        {
            Unbind();
            _button = GetComponent<Button>();
            if (_label == null) _label = GetComponentInChildren<TMP_Text>(true);
            _viewModel = viewModel;
            _execute = execute;
            _button.onClick.AddListener(OnClicked);
            _subscription = viewModel.CommandsVersion.Subscribe(_ => Refresh());
        }
        public void Unbind()
        {
            _subscription?.Dispose(); _subscription = null;
            if (_button != null) { _button.onClick.RemoveListener(OnClicked); _button.interactable = false; }
            _viewModel = null; _execute = null;
        }
        private void Refresh()
        {
            if (_definition == null || _viewModel == null)
            {
                _button.interactable = false;
                if (_label != null) _label.text = "Unassigned";
                if (_description != null) _description.text = string.Empty;
                return;
            }
            var state = _viewModel.GetCommandState(_definition.Action);
            _button.interactable = isActiveAndEnabled && state.CanExecute;
            if (_label != null) _label.text = state.Cooldown > 0
                ? $"{_definition.DisplayName} (CD {state.Cooldown})" : _definition.DisplayName;
            if (_description != null) _description.text = _definition.Description;
        }
        private void OnClicked()
        {
            // Validate again at click time; a visual state update alone is not authorization.
            if (isActiveAndEnabled && _definition != null && _viewModel != null &&
                _viewModel.GetCommandState(_definition.Action).CanExecute)
                _execute?.Invoke(_definition.Action);
        }
        private void Reset() => _label = GetComponentInChildren<TMP_Text>(true);
        private void OnEnable() { if (_button != null && _viewModel != null) Refresh(); }
        private void OnDisable() { if (_button != null) _button.interactable = false; }
        private void OnDestroy() => Unbind();
    }
}

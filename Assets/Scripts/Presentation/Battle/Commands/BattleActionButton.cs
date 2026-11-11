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
        [SerializeField] private BattleAction _action;
        [SerializeField] private TMP_Text _label;
        private Button _button;
        private BattleViewModel _viewModel;
        private Action<BattleAction> _execute;
        private IDisposable _subscription;
        private bool _componentsCached;

        public void Bind(BattleViewModel viewModel, Action<BattleAction> execute)
        {
            Unbind();
            CacheComponents();
            _viewModel = viewModel;
            _execute = execute;
            _button.onClick.AddListener(OnClicked);
            _subscription = viewModel.CommandsVersion.Subscribe(_ => Refresh());
        }

        public void Unbind()
        {
            _subscription?.Dispose();
            _subscription = null;
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClicked);
                _button.interactable = false;
            }

            _viewModel = null;
            _execute = null;
        }

        private void Awake() => CacheComponents();

        private void CacheComponents()
        {
            if (_componentsCached) return;
            _button = GetComponent<Button>();
            if (_label == null) _label = GetComponentInChildren<TMP_Text>(true);
            _componentsCached = true;
        }

        private void Refresh()
        {
            if (_viewModel == null)
            {
                _button.interactable = false;
                if (_label != null) _label.text = "Unassigned";
                return;
            }

            var state = _viewModel.GetCommandState(_action);
            _button.interactable = isActiveAndEnabled && state.CanExecute;
            if (_label != null)
                _label.text = state.Cooldown > 0
                    ? _viewModel.CooldownLabel(_action, state.Cooldown)
                    : _viewModel.ActionLabel(_action);
        }

        private void OnClicked()
        {
            if (isActiveAndEnabled && _viewModel != null &&
                _viewModel.GetCommandState(_action).CanExecute)
                _execute?.Invoke(_action);
        }

        private void Reset() => _label = GetComponentInChildren<TMP_Text>(true);

        private void OnEnable()
        {
            if (_button != null && _viewModel != null) Refresh();
        }

        private void OnDisable()
        {
            if (_button != null) _button.interactable = false;
        }

        private void OnDestroy() => Unbind();
    }
}
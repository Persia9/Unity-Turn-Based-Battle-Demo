using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Presentation.Battle.Performance
{
    public sealed class PerformanceView : MonoBehaviour
    {
        [SerializeField] private Button _toggleButton;
        [SerializeField] private TMP_Text _toggleLabel;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _label;
        private PerformanceViewModel _viewModel;
        private CompositeDisposable _bindings;

        [Inject]
        private void Construct(PerformanceViewModel viewModel)
        {
            _viewModel = viewModel;
            if (isActiveAndEnabled) Bind();
        }

        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            gameObject.SetActive(false);
#endif
        }

        private void OnEnable()
        {
            if (_viewModel != null) Bind();
        }

        private void Bind()
        {
            Unbind();
            _bindings = new CompositeDisposable();
            _viewModel.IsVisible.Subscribe(value => _panel.SetActive(value)).AddTo(_bindings);
            _viewModel.ToggleLabel.Subscribe(value => _toggleLabel.text = value).AddTo(_bindings);
            _viewModel.Text.Subscribe(value => _label.text = value).AddTo(_bindings);
            _toggleButton.onClick.AddListener(Toggle);
        }

        private void Toggle() => _viewModel.Toggle();
        private void Update() => _viewModel?.Tick(Time.unscaledDeltaTime);

        private void Unbind()
        {
            _toggleButton.onClick.RemoveListener(Toggle);
            _bindings?.Dispose();
            _bindings = null;
        }

        private void OnDisable() => Unbind();
        private void OnDestroy() => Unbind();
    }
}
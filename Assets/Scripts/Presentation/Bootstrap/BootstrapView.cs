using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Presentation.Bootstrap
{
    public sealed class BootstrapView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private TMP_Text _errorText;
        [SerializeField] private Image _progressFill;
        [Inject] private BootstrapViewModel _viewModel;
        private CompositeDisposable _bindings;

        private void Start()
        {
            _bindings = new CompositeDisposable();
            _viewModel.Status.Subscribe(value => _statusText.text = value).AddTo(_bindings);
            _viewModel.Progress.Subscribe(value => _progressText.text = value).AddTo(_bindings);
            _viewModel.Error.Subscribe(value => _errorText.text = value).AddTo(_bindings);
            _viewModel.DownloadProgress.Subscribe(value => _progressFill.fillAmount = value).AddTo(_bindings);
            _viewModel.Run(destroyCancellationToken).Forget();
        }
        private void OnDestroy()
        {
            _bindings?.Dispose();
        }
    }
}

using System;
using TMPro;
using UnityEngine;

namespace Startup
{
    public sealed class StartupScreen : MonoBehaviour, ILoadingScreen
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private TMP_Text _status;

        public void Show(string message)
        {
            _status.text = message;
            _canvas.enabled = true;
        }
        public void Hide() => _canvas.enabled = false;
        public void Fail(Exception exception)
        {
            Debug.LogException(exception);
            Show("Unable to start. Restart the game to retry.\n" + exception.Message);
        }
    }
}

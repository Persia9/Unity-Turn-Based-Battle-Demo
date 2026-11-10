using UnityEngine;

namespace Infrastructure.Scene
{
    public sealed class SceneLoadingOverlay : MonoBehaviour, ISceneLoadingOverlay
    {
        [SerializeField] private Canvas _canvas;

        public void Show() => _canvas.enabled = true;
        public void Hide() => _canvas.enabled = false;
    }
}

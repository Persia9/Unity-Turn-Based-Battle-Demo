using Cysharp.Threading.Tasks;
using Infrastructure.Asset;
using UnityEngine;
using Zenject;

namespace Infrastructure.UI
{
    public sealed class UIViewFactory : IUIViewFactory
    {
        private readonly IAssetService _assetService;
        private readonly DiContainer _container;

        public UIViewFactory(IAssetService assetService, DiContainer container)
        {
            _assetService = assetService;
            _container = container;
        }

        public async UniTask<T> Create<T>(string prefabKey) where T : UIView
        {
            var prefab = await _assetService.Load<GameObject>(prefabKey);
            var instance = _container.InstantiatePrefab(prefab);
            return instance.GetComponent<T>();
        }
    }
}
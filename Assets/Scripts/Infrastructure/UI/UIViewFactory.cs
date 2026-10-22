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
            GameObject instance = null;
            try
            {
                instance = _container.InstantiatePrefab(prefab);
                var view = instance.GetComponent<T>();
                if (view == null) throw new System.InvalidOperationException($"{prefabKey} has no {typeof(T).Name}.");
                // Keep the loaded prefab alive for as long as its clone exists.
                instance.AddComponent<UIViewAssetLease>().Initialize(_assetService, prefab);
                return view;
            }
            catch
            {
                if (instance != null) Object.Destroy(instance);
                await _assetService.Release(prefab);
                throw;
            }
        }
    }
}

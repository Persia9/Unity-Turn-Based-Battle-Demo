using Cysharp.Threading.Tasks;
using Infrastructure.Asset;
using UnityEngine;

namespace Infrastructure.UI
{
    public sealed class UIViewAssetLease : MonoBehaviour
    {
        private IAssetService _assets;
        private Object _prefab;
        public void Initialize(IAssetService assets, Object prefab) { _assets = assets; _prefab = prefab; }
        private void OnDestroy()
        {
            if (_assets != null && _prefab != null) _assets.Release(_prefab).Forget();
        }
    }
}

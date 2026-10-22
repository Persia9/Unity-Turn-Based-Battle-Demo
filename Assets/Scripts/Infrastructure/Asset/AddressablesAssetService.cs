using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Infrastructure.Asset
{
    public sealed class AddressableAssetService : IAssetService
    {
        public async UniTask<T> Load<T>(string key) where T : Object
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            try
            {
                return await handle.ToUniTask<T>();
            }
            catch
            {
                // Successful loads are released by their owner; failed loads have no owner.
                if (handle.IsValid()) Addressables.Release(handle);
                throw;
            }
        }

        public async UniTask Release(object assetOrInstance)
        {
            if (assetOrInstance == null)
            {
                await UniTask.CompletedTask;
                return;
            }

            Addressables.Release(assetOrInstance);
            await UniTask.CompletedTask;
        }
    }
}

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Infrastructure.Asset
{
    public sealed class AddressableAssetService : IAssetService
    {
        public async UniTask<T> Load<T>(string key) where T : Object
        {
            return await Addressables.LoadAssetAsync<T>(key);
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
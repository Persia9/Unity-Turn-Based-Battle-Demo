using Cysharp.Threading.Tasks;
using MasterMemory;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Infrastructure.MasterData
{
    public sealed class AddressableMasterDatabaseLoader : IMasterDatabaseLoader
    {
        public async UniTask<MemoryDatabase> Load(string key)
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(key);
            try
            {
                var text = await handle.ToUniTask<TextAsset>();
                return new MemoryDatabase(text.bytes);
            }
            finally { if (handle.IsValid()) Addressables.Release(handle); }
        }
    }
}

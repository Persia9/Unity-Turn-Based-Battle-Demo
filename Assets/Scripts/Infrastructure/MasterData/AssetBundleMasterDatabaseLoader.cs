using Cysharp.Threading.Tasks;
using Infrastructure.Asset;
using MasterMemory;
using UnityEngine;

namespace Infrastructure.MasterData
{
    public sealed class AssetBundleMasterDatabaseLoader : IMasterDatabaseLoader
    {
        private readonly IAssetService _assets;

        public AssetBundleMasterDatabaseLoader(IAssetService assets) => _assets = assets;

        public async UniTask<MemoryDatabase> Load(string key)
        {
            var text = await _assets.Load<TextAsset>(key);
            try { return new MemoryDatabase(text.bytes); }
            finally { await _assets.Release(text); }
        }
    }
}

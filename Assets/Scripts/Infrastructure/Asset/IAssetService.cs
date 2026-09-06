using Cysharp.Threading.Tasks;

namespace Infrastructure.Asset
{
    public interface IAssetService
    {
        UniTask<T> Load<T>(string key) where T : UnityEngine.Object;
        UniTask Release(object assetOrInstance);
    }
}
using UnityEngine;

namespace Infrastructure.MasterData.Editor
{
    [CreateAssetMenu(menuName = "Battle Demo/Master Data Source", fileName = "DemoMasterData")]
    public sealed class DemoMasterDataSource : ScriptableObject
    {
        public MasterDataSourceData Data = new();
    }
}
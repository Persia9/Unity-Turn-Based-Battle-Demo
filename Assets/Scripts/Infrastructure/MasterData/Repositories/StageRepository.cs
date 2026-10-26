using Infrastructure.MasterData.Masters;

namespace Infrastructure.MasterData.Repositories
{
    public sealed class StageRepository : IStageRepository
    {
        private readonly IMasterDatabaseProvider _provider;
        public StageRepository(IMasterDatabaseProvider provider) => _provider = provider;
        public StageMaster Get(int id)
        {
            if (_provider.CoreDb.StageMasterTable.TryFindById(id, out var value)) return value;
            throw new MasterDataNotFoundException("StageMaster", id);
        }
    }
}

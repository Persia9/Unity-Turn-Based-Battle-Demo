using Infrastructure.MasterData.Masters;

namespace Infrastructure.MasterData.Repositories
{
    public sealed class StageRepository : IStageRepository
    {
        private readonly IMasterDatabaseProvider _provider;
        public StageRepository(IMasterDatabaseProvider provider) => _provider = provider;
        public StageMaster Get(int id) => _provider.CoreDb.StageMasterTable.FindById(id);
    }
}
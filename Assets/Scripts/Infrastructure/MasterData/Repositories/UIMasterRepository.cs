using Infrastructure.MasterData.Masters;

namespace Infrastructure.MasterData.Repositories
{
    public sealed class UIMasterRepository : IUIMasterRepository
    {
        private readonly IMasterDatabaseProvider _provider;
        public UIMasterRepository(IMasterDatabaseProvider provider) => _provider = provider;
        public UIMaster Get(string id) => _provider.UiDb.UIMasterTable.FindById(id);
    }
}
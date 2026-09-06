using Infrastructure.MasterData.Masters;

namespace Infrastructure.MasterData.Repositories
{
    public sealed class AudioEventRepository : IAudioEventRepository
    {
        private readonly IMasterDatabaseProvider _provider;
        public AudioEventRepository(IMasterDatabaseProvider provider) => _provider = provider;
        public AudioEventMaster Get(string id) => _provider.AudioDb.AudioEventMasterTable.FindById(id);
    }
}
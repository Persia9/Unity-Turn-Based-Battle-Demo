using Infrastructure.MasterData.Masters;

namespace Infrastructure.MasterData.Repositories
{
    public sealed class CharacterRepository : ICharacterRepository
    {
        private readonly IMasterDatabaseProvider _provider;
        public CharacterRepository(IMasterDatabaseProvider provider) => _provider = provider;
        public CharacterMaster Get(int id) => _provider.CoreDb.CharacterMasterTable.FindById(id);
    }
}
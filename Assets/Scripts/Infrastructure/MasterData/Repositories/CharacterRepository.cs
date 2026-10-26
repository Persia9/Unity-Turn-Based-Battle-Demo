using Infrastructure.MasterData.Masters;

namespace Infrastructure.MasterData.Repositories
{
    public sealed class CharacterRepository : ICharacterRepository
    {
        private readonly IMasterDatabaseProvider _provider;
        public CharacterRepository(IMasterDatabaseProvider provider) => _provider = provider;
        public CharacterMaster Get(int id)
        {
            if (_provider.CoreDb.CharacterMasterTable.TryFindById(id, out var value)) return value;
            throw new MasterDataNotFoundException("CharacterMaster", id);
        }
    }
}

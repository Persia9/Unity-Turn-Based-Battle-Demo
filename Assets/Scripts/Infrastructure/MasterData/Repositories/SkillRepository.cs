using Infrastructure.MasterData.Masters;

namespace Infrastructure.MasterData.Repositories
{
    public sealed class SkillRepository : ISkillRepository
    {
        private readonly IMasterDatabaseProvider _provider;
        public SkillRepository(IMasterDatabaseProvider provider) => _provider = provider;
        public SkillMaster Get(int id)
        {
            if (_provider.CoreDb.SkillMasterTable.TryFindById(id, out var value)) return value;
            throw new MasterDataNotFoundException("SkillMaster", id);
        }
    }
}

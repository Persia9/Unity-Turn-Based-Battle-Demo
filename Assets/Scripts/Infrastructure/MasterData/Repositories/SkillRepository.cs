using Infrastructure.MasterData.Masters;

namespace Infrastructure.MasterData.Repositories
{
    public sealed class SkillRepository : ISkillRepository
    {
        private readonly IMasterDatabaseProvider _provider;
        public SkillRepository(IMasterDatabaseProvider provider) => _provider = provider;
        public SkillMaster Get(int id) => _provider.CoreDb.SkillMasterTable.FindById(id);
    }
}
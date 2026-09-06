using Infrastructure.MasterData.Masters;

namespace Infrastructure.MasterData.Repositories
{
    public interface ISkillRepository
    {
        SkillMaster Get(int id);
    }
}


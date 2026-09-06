using Infrastructure.MasterData.Masters;

namespace Infrastructure.MasterData.Repositories
{
    public interface IStageRepository
    {
        StageMaster Get(int id);
    }
}
using Infrastructure.MasterData.Masters;

namespace Infrastructure.MasterData.Repositories
{
    public interface IUIMasterRepository
    {
        UIMaster Get(string id);
    }
}
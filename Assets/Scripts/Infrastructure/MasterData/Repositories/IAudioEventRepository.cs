using Infrastructure.MasterData.Masters;

namespace Infrastructure.MasterData.Repositories
{
    public interface IAudioEventRepository
    {
        AudioEventMaster Get(string id);
    }
}
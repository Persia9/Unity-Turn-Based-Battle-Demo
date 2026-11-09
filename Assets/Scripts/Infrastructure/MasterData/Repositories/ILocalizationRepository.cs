using Infrastructure.MasterData.Masters;

namespace Infrastructure.MasterData.Repositories
{
    public interface ILocalizationRepository
    {
        bool TryGet(string key, out LocalizationMaster value);
    }
}

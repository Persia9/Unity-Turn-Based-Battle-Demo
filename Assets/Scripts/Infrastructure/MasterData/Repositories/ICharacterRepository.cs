using Infrastructure.MasterData.Masters;

namespace Infrastructure.MasterData.Repositories
{
    public interface ICharacterRepository
    {
        CharacterMaster Get(int id);
    }
}
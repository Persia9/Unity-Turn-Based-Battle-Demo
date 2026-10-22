using Cysharp.Threading.Tasks;
using MasterMemory;

namespace Infrastructure.MasterData
{
    public interface IMasterDatabaseLoader
    {
        UniTask<MemoryDatabase> Load(string key);
    }
}

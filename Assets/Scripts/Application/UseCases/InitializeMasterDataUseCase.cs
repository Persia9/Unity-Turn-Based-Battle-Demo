using Cysharp.Threading.Tasks;
using Infrastructure.MasterData;

namespace Application.UseCases
{
    public sealed class InitializeMasterDataUseCase
    {
        private readonly MasterDatabaseProvider _provider;

        public InitializeMasterDataUseCase(MasterDatabaseProvider provider)
        {
            _provider = provider;
        }

        public UniTask Execute()
        {
            return _provider.InitializeAsync();
        }
    }
}
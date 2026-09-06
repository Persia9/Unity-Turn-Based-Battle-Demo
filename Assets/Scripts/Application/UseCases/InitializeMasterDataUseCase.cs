using Cysharp.Threading.Tasks;
using Infrastructure.MasterData;

namespace Application.UseCases
{
    public sealed class InitializeMasterDataUseCase
    {
        private readonly AddressableMasterDatabaseProvider _provider;

        public InitializeMasterDataUseCase(AddressableMasterDatabaseProvider provider)
        {
            _provider = provider;
        }

        public UniTask Execute()
        {
            return _provider.InitializeAsync();
        }
    }
}
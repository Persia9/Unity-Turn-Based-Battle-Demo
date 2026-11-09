using System;
using Infrastructure.MasterData.Masters;

namespace Infrastructure.MasterData.Repositories
{
    public sealed class LocalizationRepository : ILocalizationRepository
    {
        private readonly IMasterDatabaseProvider _provider;
        public LocalizationRepository(IMasterDatabaseProvider provider) => _provider = provider;
        public bool TryGet(string key, out LocalizationMaster value)
        {
            if (!_provider.IsLocalizationReady)
                throw new InvalidOperationException("Localization master is not loaded. Await InitializeMasterDataUseCase before requesting text.");
            return _provider.LocalizationDb.LocalizationMasterTable.TryFindByKey(key, out value);
        }
    }
}

using Infrastructure.MasterData.Repositories;

namespace Infrastructure.UI
{
    public sealed class UIDefinitionRepository : IUIDefinitionRepository
    {
        private readonly IUIMasterRepository _repository;

        public UIDefinitionRepository(IUIMasterRepository repository)
        {
            _repository = repository;
        }

        public UIDefinition Get(string id)
        {
            var master = _repository.Get(id);
            return new UIDefinition(
                master.Id,
                master.PrefabKey,
                (UILayer)master.Layer,
                (UIType)master.Type,
                master.Cache,
                master.Singleton,
                master.BlockInput);
        }
    }
}
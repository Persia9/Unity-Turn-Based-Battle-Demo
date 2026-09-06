using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Infrastructure.UI
{
    public sealed class UIService : IUIService
    {
        private readonly IUIDefinitionRepository _definitionRepository;
        private readonly IUIViewFactory _factory;
        private readonly IUILayerService _layerService;
        private readonly UIRegistry _registry;

        public UIService(
            IUIDefinitionRepository definitionRepository,
            IUIViewFactory factory,
            IUILayerService layerService,
            UIRegistry registry)
        {
            _definitionRepository = definitionRepository;
            _factory = factory;
            _layerService = layerService;
            _registry = registry;
        }

        public async UniTask<T> Open<T>(string uiId) where T : UIView
        {
            var definition = _definitionRepository.Get(uiId);

            if (definition.Singleton && _registry.TryGet(uiId, out var opened))
            {
                return opened as T;
            }

            var view = await _factory.Create<T>(definition.PrefabKey);
            view.transform.SetParent(_layerService.GetLayerRoot(definition.Layer), false);

            _registry.Register(uiId, view);
            view.OnOpened();
            return view;
        }

        public async UniTask<TView> Open<TView, TContext>(string uiId, TContext context)
            where TView : UIView, IUIContextReceiver<TContext>
        {
            var definition = _definitionRepository.Get(uiId);

            if (definition.Singleton && _registry.TryGet(uiId, out var opened))
            {
                var existing = opened as TView;
                existing.SetContext(context);
                return existing;
            }

            var view = await _factory.Create<TView>(definition.PrefabKey);
            view.transform.SetParent(_layerService.GetLayerRoot(definition.Layer), false);
            view.SetContext(context);

            _registry.Register(uiId, view);
            view.OnOpened();
            return view;
        }

        public async UniTask Close(string uiId)
        {
            if (!_registry.TryGet(uiId, out var view))
            {
                await UniTask.CompletedTask;
                return;
            }

            view.OnClosed();
            Object.Destroy(view.gameObject);
            _registry.Unregister(uiId);
            await UniTask.CompletedTask;
        }

        public bool IsOpened(string uiId) => _registry.IsOpened(uiId);
    }
}
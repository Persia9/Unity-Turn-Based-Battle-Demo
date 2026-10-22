using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Infrastructure.UI
{
    // Scene scoped: cached views and in-flight loads cannot escape their scene.
    public sealed class UIService : IUIService, IDisposable
    {
        private readonly IUIDefinitionRepository _definitions;
        private readonly IUIViewFactory _factory;
        private readonly IUILayerService _layers;
        private readonly UIRegistry _registry;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly Dictionary<string, Stack<UIView>> _cache = new();
        private readonly Dictionary<string, List<UIView>> _opened = new();
        private readonly Dictionary<UIView, GameObject> _blockers = new();
        private bool _disposed;

        public UIService(IUIDefinitionRepository definitions, IUIViewFactory factory,
            IUILayerService layers, UIRegistry registry)
        { _definitions = definitions; _factory = factory; _layers = layers; _registry = registry; }

        public UniTask<T> Open<T>(string uiId) where T : UIView => OpenInternal<T>(uiId, null);
        public UniTask<T> Open<T, TContext>(string uiId, TContext context)
            where T : UIView, IUIContextReceiver<TContext>
            => OpenInternal<T>(uiId, view => view.SetContext(context));

        private async UniTask<T> OpenInternal<T>(string id, Action<T> setContext) where T : UIView
        {
            await _gate.WaitAsync();
            UIView created = null;
            try
            {
                if (_disposed) throw new ObjectDisposedException(nameof(UIService));
                var definition = _definitions.Get(id);
                if (definition.Singleton && _registry.TryGet(id, out var existing) && existing != null)
                {
                    var typed = existing as T ?? throw new InvalidOperationException($"UI type mismatch: {id}");
                    if (setContext != null) { typed.OnClosed(); setContext(typed); typed.OnOpened(); }
                    return typed;
                }
                T view = null;
                if (_cache.TryGetValue(id, out var cached))
                    while (cached.Count > 0 && view == null) view = cached.Pop() as T;
                if (view == null) view = await _factory.Create<T>(definition.PrefabKey);
                created = view;
                if (_disposed) throw new ObjectDisposedException(nameof(UIService));
                view.transform.SetParent(_layers.GetLayerRoot(definition.Layer), false);
                if (definition.BlockInput)
                {
                    var blocker = new GameObject($"{id} InputBlocker", typeof(RectTransform), typeof(Image));
                    blocker.transform.SetParent(view.transform.parent, false);
                    var rect = (RectTransform)blocker.transform;
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    blocker.GetComponent<Image>().color = new Color(0, 0, 0, 0.45f);
                    _blockers.Add(view, blocker);
                }
                view.transform.SetAsLastSibling();
                setContext?.Invoke(view);
                view.gameObject.SetActive(true);
                view.OnOpened();
                if (!_opened.TryGetValue(id, out var views)) _opened[id] = views = new List<UIView>();
                views.Add(view);
                _registry.Register(id, view);
                return view;
            }
            catch
            {
                if (created != null) { RemoveBlocker(created); Object.Destroy(created.gameObject); }
                throw;
            }
            finally { _gate.Release(); }
        }

        public async UniTask Close(string id)
        {
            await _gate.WaitAsync();
            try
            {
                if (_disposed || !_opened.TryGetValue(id, out var views) || views.Count == 0) return;
                var view = views[views.Count - 1]; views.RemoveAt(views.Count - 1);
                if (views.Count == 0) _registry.Unregister(id);
                else _registry.Register(id, views[views.Count - 1]);
                if (view == null) return;
                view.OnClosed(); RemoveBlocker(view);
                if (_definitions.Get(id).Cache)
                {
                    view.gameObject.SetActive(false);
                    if (!_cache.TryGetValue(id, out var cached)) _cache[id] = cached = new Stack<UIView>();
                    cached.Push(view);
                }
                else Object.Destroy(view.gameObject);
            }
            finally { _gate.Release(); }
        }

        private void RemoveBlocker(UIView view)
        {
            if (_blockers.TryGetValue(view, out var blocker)) Object.Destroy(blocker);
            _blockers.Remove(view);
        }
        public bool IsOpened(string id) => !_disposed && _registry.IsOpened(id);
        public void Dispose()
        {
            _disposed = true;
            foreach (var views in _opened.Values)
                foreach (var view in views)
                    if (view != null) { view.OnClosed(); Object.Destroy(view.gameObject); }
            foreach (var views in _cache.Values)
                foreach (var view in views) if (view != null) Object.Destroy(view.gameObject);
            foreach (var blocker in _blockers.Values) if (blocker != null) Object.Destroy(blocker);
            _opened.Clear(); _cache.Clear(); _blockers.Clear();
        }
    }
}

using System.Collections.Generic;

namespace Infrastructure.UI
{
    public sealed class UIRegistry
    {
        private readonly Dictionary<string, UIView> _opened = new();

        public bool TryGet(string id, out UIView view) => _opened.TryGetValue(id, out view);
        public bool IsOpened(string id) => _opened.ContainsKey(id);
        public void Register(string id, UIView view) => _opened[id] = view;
        public void Unregister(string id) => _opened.Remove(id);
    }
}
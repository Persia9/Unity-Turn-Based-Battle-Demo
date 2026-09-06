using System;
using System.Collections.Generic;
using UnityEngine;

namespace Infrastructure.UI
{
    public sealed class UILayerService : IUILayerService
    {
        private readonly Dictionary<UILayer, Transform> _roots;

        public UILayerService(Transform hud, Transform screen, Transform popup, Transform overlay, Transform top)
        {
            _roots = new Dictionary<UILayer, Transform>
            {
                [UILayer.Hud] = hud,
                [UILayer.Screen] = screen,
                [UILayer.Popup] = popup,
                [UILayer.Overlay] = overlay,
                [UILayer.Top] = top,
            };
        }

        public Transform GetLayerRoot(UILayer layer)
        {
            if (_roots.TryGetValue(layer, out var root)) return root;
            throw new InvalidOperationException($"Layer root not found: {layer}");
        }
    }
}
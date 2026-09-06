using UnityEngine;

namespace Infrastructure.UI
{
    public interface IUILayerService
    {
        Transform GetLayerRoot(UILayer layer);
    }
}
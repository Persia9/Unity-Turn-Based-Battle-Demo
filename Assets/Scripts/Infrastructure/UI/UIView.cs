using UnityEngine;

namespace Infrastructure.UI
{
    public abstract class UIView : MonoBehaviour
    {
        public virtual void OnOpened()
        {
        }

        public virtual void OnClosed()
        {
        }
    }
}
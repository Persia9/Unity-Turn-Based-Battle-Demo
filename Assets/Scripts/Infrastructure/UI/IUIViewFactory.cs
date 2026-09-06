using Cysharp.Threading.Tasks;

namespace Infrastructure.UI
{
    public interface IUIViewFactory
    {
        UniTask<T> Create<T>(string prefabKey) where T : UIView;
    }
}
using Cysharp.Threading.Tasks;

namespace Infrastructure.UI
{
    public interface IUIService
    {
        UniTask<T> Open<T>(string uiId) where T : UIView;

        UniTask<TView> Open<TView, TContext>(string uiId, TContext context)
            where TView : UIView, IUIContextReceiver<TContext>;

        UniTask Close(string uiId);
        bool IsOpened(string uiId);
    }
}
using Cysharp.Threading.Tasks;

namespace Infrastructure.UI
{
    public interface IScreenService
    {
        UniTask<TView> OpenScreen<TView, TContext>(string uiId, TContext context)
            where TView : UIView, IUIContextReceiver<TContext>;

        UniTask CloseScreen(string uiId);
    }
}
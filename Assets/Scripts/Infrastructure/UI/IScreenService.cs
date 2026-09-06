using Cysharp.Threading.Tasks;

namespace Infrastructure.UI
{
    public interface IPopupService
    {
        UniTask<TView> OpenPopup<TView, TContext>(string uiId, TContext context)
            where TView : UIView, IUIContextReceiver<TContext>;

        UniTask ClosePopup(string uiId);
    }
}
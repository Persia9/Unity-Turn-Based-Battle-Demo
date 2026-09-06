using Cysharp.Threading.Tasks;

namespace Infrastructure.UI
{
    public sealed class PopupService : IPopupService
    {
        private readonly IUIService _uiService;

        public PopupService(IUIService uiService)
        {
            _uiService = uiService;
        }

        public UniTask<TView> OpenPopup<TView, TContext>(string uiId, TContext context)
            where TView : UIView, IUIContextReceiver<TContext>
        {
            // 通常の UI と異なる振る舞い
            // todo
            
            return _uiService.Open<TView, TContext>(uiId, context);
        }

        public UniTask ClosePopup(string uiId)
        {
            return _uiService.Close(uiId);
        }
    }
}
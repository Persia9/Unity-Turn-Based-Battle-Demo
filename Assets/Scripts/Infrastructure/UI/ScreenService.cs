using Cysharp.Threading.Tasks;

namespace Infrastructure.UI
{
    public sealed class ScreenService : IScreenService
    {
        private readonly IUIService _uiService;

        public ScreenService(IUIService uiService)
        {
            _uiService = uiService;
        }

        public UniTask<TView> OpenScreen<TView, TContext>(string uiId, TContext context)
            where TView : UIView, IUIContextReceiver<TContext>
        {
            // 通常の UI と異なる振る舞い
            // todo

            return _uiService.Open<TView, TContext>(uiId, context);
        }

        public UniTask CloseScreen(string uiId)
        {
            return _uiService.Close(uiId);
        }
    }
}
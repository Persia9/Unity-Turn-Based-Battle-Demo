using Cysharp.Threading.Tasks;

namespace Infrastructure.UI
{
    public sealed class PopupService : IPopupService
    {
        private readonly IUIService _uiService;
        private readonly IUIDefinitionRepository _definitions;

        public PopupService(IUIService uiService, IUIDefinitionRepository definitions)
        {
            _uiService = uiService;
            _definitions = definitions;
        }

        public UniTask<TView> OpenPopup<TView, TContext>(string uiId, TContext context)
            where TView : UIView, IUIContextReceiver<TContext>
        {
            if (_definitions.Get(uiId).Type != UIType.Popup)
                throw new System.InvalidOperationException($"{uiId} is not a popup.");
            return _uiService.Open<TView, TContext>(uiId, context);
        }

        public UniTask ClosePopup(string uiId)
        {
            return _uiService.Close(uiId);
        }
    }
}

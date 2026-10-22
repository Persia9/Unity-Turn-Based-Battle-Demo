using Cysharp.Threading.Tasks;

namespace Infrastructure.UI
{
    public sealed class ScreenService : IScreenService
    {
        private readonly IUIService _uiService;
        private readonly IUIDefinitionRepository _definitions;

        public ScreenService(IUIService uiService, IUIDefinitionRepository definitions)
        {
            _uiService = uiService;
            _definitions = definitions;
        }

        public UniTask<TView> OpenScreen<TView, TContext>(string uiId, TContext context)
            where TView : UIView, IUIContextReceiver<TContext>
        {
            if (_definitions.Get(uiId).Type != UIType.Screen)
                throw new System.InvalidOperationException($"{uiId} is not a screen.");
            return _uiService.Open<TView, TContext>(uiId, context);
        }

        public UniTask CloseScreen(string uiId)
        {
            return _uiService.Close(uiId);
        }
    }
}

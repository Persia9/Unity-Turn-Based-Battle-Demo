using Cysharp.Threading.Tasks;
using Infrastructure.UI;
using Presentation.Battle.Result;

namespace Application.UseCases
{
    public sealed class OpenResultPopupUseCase
    {
        private readonly IPopupService _popupService;

        public OpenResultPopupUseCase(IPopupService popupService)
        {
            _popupService = popupService;
        }

        public UniTask Execute(bool isWin)
        {
            return _popupService.OpenPopup<ResultPopupView, ResultPopupContext>(
                "ResultPopup",
                new ResultPopupContext(isWin));
        }
    }
}
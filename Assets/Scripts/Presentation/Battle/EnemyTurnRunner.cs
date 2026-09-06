using Application.UseCases;
using Cysharp.Threading.Tasks;

namespace Presentation.Battle
{
    public sealed class EnemyTurnRunner
    {
        private readonly AttackUseCase _attackUseCase;

        public EnemyTurnRunner(AttackUseCase attackUseCase)
        {
            _attackUseCase = attackUseCase;
        }

        public async UniTask RunAsync()
        {
            await UniTask.Delay(700);
            _attackUseCase.ExecuteEnemyAttack();
        }
    }
}
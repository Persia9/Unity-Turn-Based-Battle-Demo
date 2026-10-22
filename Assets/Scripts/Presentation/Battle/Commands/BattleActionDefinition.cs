using Domain.Battle;
using UnityEngine;

namespace Presentation.Battle.Commands
{
    [CreateAssetMenu(menuName = "Battle Demo/Action Definition", fileName = "ActionDefinition")]
    public sealed class BattleActionDefinition : ScriptableObject
    {
        [SerializeField] private BattleAction _action;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        public BattleAction Action => _action;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public string Description => _description;
    }
}

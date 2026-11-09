namespace Infrastructure.UI
{
    public sealed class UIDefinition
    {
        public string Id { get; }
        public string PrefabKey { get; }

        public UILayer Layer => Type switch
        {
            UIType.Hud => UILayer.Hud,
            UIType.Screen => UILayer.Screen,
            UIType.Popup => UILayer.Popup,
            UIType.Overlay => UILayer.Overlay,
            UIType.Toast => UILayer.Top,
            _ => throw new System.ArgumentOutOfRangeException(nameof(Type), Type, "Unknown UI type.")
        };

        public UIType Type { get; }
        public bool Cache { get; }
        public bool BlockInput { get; }

        public UIDefinition(
            string id,
            string prefabKey,
            UIType type,
            bool cache,
            bool blockInput)
        {
            Id = id;
            PrefabKey = prefabKey;
            Type = type;
            Cache = cache;
            BlockInput = blockInput;
        }
    }
}
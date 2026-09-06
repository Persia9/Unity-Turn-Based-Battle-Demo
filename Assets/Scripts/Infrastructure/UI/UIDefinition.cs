namespace Infrastructure.UI
{
    public sealed class UIDefinition
    {
        public string Id { get; }
        public string PrefabKey { get; }
        public UILayer Layer { get; }
        public UIType Type { get; }
        public bool Cache { get; }
        public bool Singleton { get; }
        public bool BlockInput { get; }

        public UIDefinition(
            string id,
            string prefabKey,
            UILayer layer,
            UIType type,
            bool cache,
            bool singleton,
            bool blockInput)
        {
            Id = id;
            PrefabKey = prefabKey;
            Layer = layer;
            Type = type;
            Cache = cache;
            Singleton = singleton;
            BlockInput = blockInput;
        }
    }
}
using System;

namespace Infrastructure.MasterData
{
    public sealed class MasterDataLoadException : Exception
    {
        public string Key { get; }
        public MasterDataLoadException(string key, Exception inner)
            : base($"Failed to load master database '{key}'. Initialization can be retried.", inner) => Key = key;
    }
}

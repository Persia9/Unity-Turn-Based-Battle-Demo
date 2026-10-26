using System;

namespace Infrastructure.MasterData
{
    public sealed class MasterDataNotFoundException : Exception
    {
        public string TableName { get; }
        public object Key { get; }

        public MasterDataNotFoundException(string tableName, object key)
            : base($"Master data was not found: table={tableName}, key={key}")
        {
            TableName = tableName;
            Key = key;
        }
    }
}

using Azure;
using Azure.Data.Tables;
using Microsoft.Graph;

namespace SPOAzBlob.Engine.Models
{
    public class PropertyBagEntry : ITableEntity
    {
        public const string PARTITION_NAME = "Properties";

        public PropertyBagEntry()
        { 
        }

        public PropertyBagEntry(string property, string value)
        {
            // Partition by drive
            this.PartitionKey = PARTITION_NAME;
            
            // Key is encoded URL
            this.RowKey = property;
            this.Value = value;
        }

        public string Value { get; set; } = string.Empty;   
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = String.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

    }
}

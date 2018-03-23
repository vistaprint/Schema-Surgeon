using System.Collections.Generic;

namespace SchemaSurgeon.ModifyColumns
{
    public class IndexSpec
    {
        public IndexSpec(
            string indexName, string databaseName, string schemaName, string tableName, string indexType, Dictionary<string, string> indexOptions, List<ColumnIndexInfo> columns, bool isDisabled)
        {
            this.DatabaseName = databaseName;
            this.SchemaName = schemaName;
            this.TableName = tableName;
            this.ConstraintName = indexName;
            this.IndexType = indexType;
            this.IndexOptions = indexOptions;
            this.Columns = columns;
            this.IsDisabled = isDisabled;
        }

        public List<ColumnIndexInfo> Columns { get; private set; }

        public string ConstraintName { get; private set; }
        
        public Dictionary<string, string> IndexOptions { get; private set; }

        public string IndexType { get; private set; }

        public string DatabaseName { get; private set; }

        public string SchemaName { get; private set; }

        public string TableName { get; private set; }

        public bool IsDisabled { get; private set; }

    }
}
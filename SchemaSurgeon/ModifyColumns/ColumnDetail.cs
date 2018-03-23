namespace SchemaSurgeon.ModifyColumns
{
    public class ColumnDetail
    {
        public ColumnDetail(string databaseName, string schemaName, string tableName, string columnName, string newDataType, bool nullable)
        {
            DatabaseName = databaseName;
            SchemaName = schemaName;
            TableName = tableName;
            ColumnName = columnName;
            NewDataType = newDataType;
            Nullable = nullable;
        }

        public readonly string DatabaseName;
        public readonly string SchemaName;
        public readonly string TableName;
        public readonly string ColumnName;
        public readonly string NewDataType;
        public readonly bool Nullable;

        public virtual string GetAlterQuery()
        {
            return $"ALTER TABLE [{DatabaseName}].[{SchemaName}].[{TableName}] ALTER COLUMN [{ColumnName}] {NewDataType} {Nullability};";
        }

        public string Nullability {
            get
            {
                return Nullable ? "NULL" : "NOT NULL";
            }
        }
    }
}

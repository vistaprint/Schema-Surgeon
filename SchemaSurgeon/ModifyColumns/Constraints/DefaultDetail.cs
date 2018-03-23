namespace SchemaSurgeon.ModifyColumns.Constraints
{
    public class DefaultDetail : ConstraintDetail
    {
        public override int Priority => 4;

        public DefaultDetail(string constraintName, string databaseName, string schemaName, string tableName, string columnName, string data)
            : base(constraintName, databaseName, schemaName, tableName)
        {
            this.ColumnName = columnName;
            this.Value = data;
        }

        public string ColumnName { get; private set; }
        public string Value { get; private set; }

        public override string GetAddQuery()
        {
            return $"ALTER TABLE [{Database}].[{Schema}].[{Table}] ADD  CONSTRAINT [{Name}]  DEFAULT ({Value}) FOR [{ColumnName}];";
        }

        public override string GetDropQuery()
        {
            return $"ALTER TABLE [{Database}].[{Schema}].[{Table}] DROP CONSTRAINT [{Name}];";
        }
    }
}

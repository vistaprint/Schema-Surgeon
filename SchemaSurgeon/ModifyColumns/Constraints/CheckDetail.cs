namespace SchemaSurgeon.ModifyColumns.Constraints
{
    public class CheckDetail : TrustableBasedConstraintDetail
    {
        public override int Priority => 5;

        public CheckDetail(string constraintName, string databaseName, string schemaName, string tableName, string columnName, string data, bool untrusted, bool isDisabled)
            : base(constraintName, databaseName, schemaName, tableName, untrusted, isDisabled)
        {
            this.ColumnName = columnName;
            this.Expression = data;
        }

        public string ColumnName { get; private set; } // Didn't find use for this property?

        public string Expression { get; private set; }


        public override string GetAddQuery()
        {
            // TODO: handle unstruted and isdisabled bools
            return $"ALTER TABLE [{Database}].[{Schema}].[{Table}] WITH CHECK ADD CONSTRAINT [{Name}] CHECK ({Expression});";
        }

        public override string GetDropQuery()
        {
            return $"ALTER TABLE [{Database}].[{Schema}].[{Table}] DROP CONSTRAINT [{Name}];";
        }
    }
}

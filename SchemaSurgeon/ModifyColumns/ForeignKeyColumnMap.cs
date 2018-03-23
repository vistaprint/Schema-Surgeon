namespace SchemaSurgeon.ModifyColumns
{
    public class ForeignKeyColumnMap
    {
        public string SourceColumn { get; private set; }
        public string TargetTable { get; private set; }
        public string TargetColumn { get; private set; }

        public ForeignKeyColumnMap(string sourceColumn, string targetTable, string targetColumn)
        {
            this.SourceColumn = sourceColumn;
            this.TargetTable = targetTable;
            this.TargetColumn = targetColumn;
        }
    }
}
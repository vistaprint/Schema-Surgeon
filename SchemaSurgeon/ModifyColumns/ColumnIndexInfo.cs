namespace SchemaSurgeon.ModifyColumns
{
    public class ColumnIndexInfo
    { 
        public ColumnIndexInfo(string columnName, bool isDescending, bool isIncludedColumn)
        {
            this.ColumnName = columnName;
            this.IsDescending = isDescending;
            this.IsIncludedColumn = isIncludedColumn;
        }    

        public string ColumnName { get; private set; }
        public bool IsDescending { get; private set; }
        public bool IsIncludedColumn { get; private set; }

    }
}
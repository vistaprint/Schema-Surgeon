using SchemaSurgeon.ModifyColumns;

namespace SchemaSurgeon.ModifyUserDefinedTableTypes
{
    class UserDefinedTableTypeColumnDetail : ColumnDetail
    {
        public UserDefinedTableTypeColumnDetail(string databaseName, string schemaName, string tableName, string columnName, string newDataType, bool nullable, int? characterMaxLength)
            : base(databaseName, schemaName, tableName, columnName, newDataType, nullable)
        {
            CharacterMaxLength = characterMaxLength;
        }

        public readonly int? CharacterMaxLength;

        public string MaxSpecifier => CharacterMaxLength != null ? $"({CharacterMaxLength})" : "";

        public override string GetAlterQuery()
        {
            return $"[{ColumnName}] [{NewDataType}]" + MaxSpecifier + " " + Nullability;
        }
    }
}

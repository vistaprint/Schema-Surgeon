using System;

namespace SchemaSurgeon.ModifyColumns
{
    internal class ColumnIdentifier
    {
        public string Database { get; }
        public string Schema { get; }
        public string Table { get; }
        public string Column { get; }

        public ColumnIdentifier(string spec)
        {
            string[] parts = spec.Split('.');

            if (parts.Length != 4)
            {
                throw new ArgumentException("spec", $"column specification '{spec}' was not in four-part database.schema.table.column format");
            }

            Database = parts[0].Trim(' ','[',']');
            Schema = parts[1].Trim(' ', '[', ']');
            Table = parts[2].Trim(' ', '[', ']');
            Column = parts[3].Trim(' ', '[', ']');

        }

        public ColumnIdentifier(string database, string schema, string spec)
        {
            var parts = spec.Split('.');

            if (parts.Length != 2)
            {
                throw new ArgumentException("spec", $"column specification '{spec}' was not in two-part table.column format");
            }

            Table = parts[0].Trim();
            Column = parts[1].Trim();
            Database = database;
            Schema = schema;
        }

        public ColumnIdentifier(string database, string schema, string table, string column)
        {
            Database = database;
            Schema = schema;
            Table = table;
            Column = column;
        }

        public override bool Equals(Object obj)
        {
            var toIdentifier = obj as ColumnIdentifier;

            if (toIdentifier == null)
            {
                return false;
            }

            return (Database == toIdentifier.Database && Schema == toIdentifier.Schema &&
                    Table == toIdentifier.Table && Column == toIdentifier.Column);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Database ?? string.Empty).GetHashCode() + (Schema ?? string.Empty).GetHashCode() +
                       (Table ?? string.Empty).GetHashCode() + (Column ?? string.Empty).GetHashCode();

            }
        }
    }
}
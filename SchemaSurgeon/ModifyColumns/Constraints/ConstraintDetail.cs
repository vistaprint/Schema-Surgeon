namespace SchemaSurgeon.ModifyColumns.Constraints
{
    public abstract class ConstraintDetail
    {
        protected ConstraintDetail(string name, string database, string schema, string table)
        {
            Name = name;
            Database = database;
            Schema = schema;
            Table = table;
        }

        public readonly string Name;
        public readonly string Database;
        public readonly string Schema;
        public readonly string Table;

        public abstract string GetAddQuery();

        public abstract string GetDropQuery();

        // A higher priority means this detail will be dropped sooner and created later (near the edges of the script)
        // A lower priority means this detail will be dropped later and created sooner (near the center of the script)
        // example
        // DROP     3
        // DROP     2
        // DROP     1
        // ALTER
        // ADD      1
        // ADD      2
        // ADD      3

        public abstract int Priority { get; }
    }
}

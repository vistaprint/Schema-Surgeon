namespace SchemaSurgeon.ModifySqlFragments
{
    public class FuncIdentifier : DatabaseSchemaObjectIdentifier
    {
        public FuncIdentifier(string database, string schema, string name) 
            : base(database, schema, name)
        {
        }

        public override string GetDropQuery()
        {
            return $"DROP FUNCTION [{Schema}].[{Name}] ";
        }
    }
}

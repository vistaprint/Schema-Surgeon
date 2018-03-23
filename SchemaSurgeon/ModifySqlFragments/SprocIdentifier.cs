namespace SchemaSurgeon.ModifySqlFragments
{
    public class SprocIdentifier : DatabaseSchemaObjectIdentifier
    {
        public SprocIdentifier(string database, string schema, string sproc) 
            : base(database, schema, sproc)
        {
        }

        public override string GetDropQuery()
        {
            return $"DROP PROCEDURE [{Schema}].[{Name}] ";
        }
    }
}

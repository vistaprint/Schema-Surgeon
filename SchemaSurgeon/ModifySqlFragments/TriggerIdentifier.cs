namespace SchemaSurgeon.ModifySqlFragments
{
    public class TriggerIdentifier : DatabaseSchemaObjectIdentifier
    {
        public TriggerIdentifier(string database, string schema, string name)
            : base(database, schema, name)
        {
        }

        public override string GetDropQuery()
        {
            return $"DROP TRIGGER [{Schema}].[{Name}]";
        }
    }
}

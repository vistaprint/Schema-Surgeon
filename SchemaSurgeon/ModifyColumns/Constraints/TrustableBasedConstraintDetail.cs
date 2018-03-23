namespace SchemaSurgeon.ModifyColumns.Constraints
{
    public abstract class TrustableBasedConstraintDetail : ConstraintDetail
    {
        public readonly bool Untrusted;
        public readonly bool IsDisabled;

        protected TrustableBasedConstraintDetail(string name, string database, string schema, string table, bool untrusted, bool isDisabled) : base(name, database, schema, table)
        {
            Untrusted = untrusted;
            IsDisabled = isDisabled;
        }
    }
}

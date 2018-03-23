namespace SchemaSurgeon.ModifyColumns.Constraints
{
    public class PrimaryKeyDetail : IndexBasedConstraintDetail
    {
        public override int Priority => 1;

        public PrimaryKeyDetail(IndexSpec index)
            : base(index)
        {
        }

        public override string GetAddQuery()
        {
            return $"ALTER TABLE [{Database}].[{Schema}].[{Table}] ADD CONSTRAINT [{Name}] PRIMARY KEY {Index.IndexType} ({CreateColumnList()}) WITH ({CreateOptionsList()});";
        }

        public override string GetDropQuery()
        {
            return $"ALTER TABLE [{Database}].[{Schema}].[{Table}] DROP CONSTRAINT [{Name}];";
        }
    }
}

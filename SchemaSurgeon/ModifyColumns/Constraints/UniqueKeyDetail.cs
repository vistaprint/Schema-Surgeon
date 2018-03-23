namespace SchemaSurgeon.ModifyColumns.Constraints
{
    public class UniqueKeyDetail : IndexBasedConstraintDetail
    {
        public override int Priority => 2;

        public UniqueKeyDetail(IndexSpec index) : base(index)
        {
        }

        public override string GetAddQuery()
        {
            return $"ALTER TABLE [{Database}].[{Schema}].[{Table}] ADD CONSTRAINT [{Name}] UNIQUE {Index.IndexType} ({CreateColumnList()}) WITH ({CreateOptionsList()});";
        }

        public override string GetDropQuery()
        {
            return $"ALTER TABLE [{Database}].[{Schema}].[{Table}] DROP CONSTRAINT [{Name}];";
        }
    }
}

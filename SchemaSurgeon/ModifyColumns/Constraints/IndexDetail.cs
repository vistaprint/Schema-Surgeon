namespace SchemaSurgeon.ModifyColumns.Constraints
{
    public class IndexDetail : IndexBasedConstraintDetail
    {
        public override int Priority => 3;
        public IndexDetail(IndexSpec index)
            : base(index)
        {
        }

        public override string GetAddQuery()
        {
            var clusterStatus = Index.IndexType;

            var columnList = CreateColumnList();
            var optionList = CreateOptionsList();

            if (HasIncludedColumns())
            {
                var includedColumnList = CreateIncludedColumnList();
                return $"CREATE {clusterStatus} INDEX [{Name}] ON [{Database}].[{Schema}].[{Table}]({columnList}) INCLUDE({includedColumnList}) WITH ({optionList});";
            }
            else
            { 
                return $"CREATE {clusterStatus} INDEX [{Name}] ON [{Database}].[{Schema}].[{Table}]({columnList}) WITH ({optionList}) ;";
            }
        }


        public override string GetDropQuery()
        {
            return $"DROP INDEX [{Name}] ON [{Database}].[{Schema}].[{Table}];";
        }
    }
}

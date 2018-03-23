using System.Collections.Generic;
using System.Linq;

namespace SchemaSurgeon.ModifyColumns.Constraints
{
    public abstract class IndexBasedConstraintDetail : ConstraintDetail
    {
        private IndexSpec _index;

        public IndexBasedConstraintDetail(IndexSpec index) : base(index.ConstraintName, index.DatabaseName, index.SchemaName, index.TableName)
        {
            _index = index;
        }

        public IndexSpec Index
        {
            get
            {
                return _index;
            }
        }
        

        protected string CreateColumnList(bool useOrdering = true)
        {
            var results = new List<string>();

            foreach (var column in Index.Columns.Where(col => ! col.IsIncludedColumn))
            {

                var columnInfo = string.Empty;

                if (useOrdering)
                {
                    var ordering = column.IsDescending ? "DESC" : "ASC";
                    columnInfo = column.ColumnName + " " + ordering;
                }
                else
                {
                    columnInfo = column.ColumnName;
                }

                results.Add(columnInfo);
            }

            return string.Join(",", results);
        }


        protected string CreateIncludedColumnList()
        {
            var includedColumns = Index.Columns.Where(col => col.IsIncludedColumn).Select(col => col.ColumnName);
            return string.Join(",", includedColumns);
        }

        protected bool HasIncludedColumns()
        {
            return Index.Columns.Any(col => col.IsIncludedColumn);
        }

        protected string CreateOptionsList()
        {
            return string.Join(",", Index.IndexOptions.Select(x => x.Key + " = " + x.Value));
        }

    }
}
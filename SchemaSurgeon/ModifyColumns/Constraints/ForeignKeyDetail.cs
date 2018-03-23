using System;
using System.Collections.Generic;
using System.Linq;

namespace SchemaSurgeon.ModifyColumns.Constraints
{
    public class ForeignKeyDetail : TrustableBasedConstraintDetail
    {
        public override int Priority => 6;

        public List<ForeignKeyColumnMap> Columns { get; private set; }
        public string DeleteAction { get; private set; }
        public string PrimaryTable { get; private set; }
        public string UpdateAction { get; private set; }

        public ForeignKeyDetail(string constraintName, string database, string schema, string primaryTable, string foreignTable, string deleteAction, string updateAction, List<ForeignKeyColumnMap> columns, bool untrusted, bool isDisabled)
            : base(constraintName, database, schema, foreignTable, untrusted, isDisabled)
        {
            this.PrimaryTable = primaryTable;
            this.DeleteAction = deleteAction;
            this.UpdateAction = updateAction;
            this.Columns = columns;

            if (DeleteAction != "NO_ACTION" || UpdateAction != "NO_ACTION")
            {
                throw new InvalidOperationException($"Can't handle foreign key {Name} which has specified Delete or Update actions");
            }
        }

        public override string GetAddQuery()
        {
            var constraintValidationOption = IsDisabled || Untrusted ? "NOCHECK" : "CHECK";

            string createConstraintCommand =
                $"ALTER TABLE [{Database}].[{Schema}].[{Table}] WITH {constraintValidationOption} " +
                $"ADD CONSTRAINT [{Name}] FOREIGN KEY ({GetSourceColumnList()}) " +
                $"REFERENCES [{PrimaryTable}] ({GetTargetColumnList()});";

            if (IsDisabled)
            {
                createConstraintCommand += $"\nALTER TABLE [{Database}].[{Schema}].[{Table}] NOCHECK CONSTRAINT [{Name}];";
            }

            return createConstraintCommand;
        }

        private string GetTargetColumnList()
        {
            return CreateColumnList(cm => cm.TargetColumn);
        }
        private string GetSourceColumnList()
        {
            return CreateColumnList(cm => cm.SourceColumn);
        }

        private string CreateColumnList(Func<ForeignKeyColumnMap, string> fieldSelector)
        {
            // Add brackets around column names to handle reserved words or invalid characters in names
            IEnumerable<string> columns = Columns.Select(map => $"[{fieldSelector(map)}]");
            return string.Join(",", columns);
        }

        public override string GetDropQuery()
        {
            return $"ALTER TABLE [{Database}].[{Schema}].[{Table}] DROP CONSTRAINT [{Name}];";
        }
    }
}

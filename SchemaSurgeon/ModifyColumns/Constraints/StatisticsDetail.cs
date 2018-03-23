using System.Collections.Generic;
using System.Linq;

namespace SchemaSurgeon.ModifyColumns.Constraints
{
    public class StatisticsDetail : ConstraintDetail
    {
        public StatisticsDetail(string name, string database, string schema, string table, IEnumerable<string> columnNames)
            : base(name, database, schema, table)
        {
            _columnNames = columnNames;
        }

        private readonly IEnumerable<string> _columnNames;

        public override int Priority => 7;

        public override string GetAddQuery()
        {
            // Put brackets around column names to escape any reserved characters
            string columns = string.Join(", ", _columnNames.Select(name => $"[{name}]"));
            return $"CREATE STATISTICS [{Name}] ON [{Database}].[{Schema}].[{Table}]({columns})";
        }

        public override string GetDropQuery()
        {
            return $"DROP STATISTICS [{Schema}].[{Table}].[{Name}]";
        }
    }
}

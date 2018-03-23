using System.Collections.Generic;
using System.Linq;

namespace SchemaSurgeon.ModifyUserDefinedTableTypes
{
    class UserDefinedTableTypeDetail
    {
        public UserDefinedTableTypeDetail(string schemaName, string tableTypeName,
            List<UserDefinedTableTypeColumnDetail> columnDetails)
        {
            _schemaName = schemaName;
            _tableTypeName = tableTypeName;
            _columnDetails = columnDetails;
        }


        private readonly string _schemaName;
        private readonly string _tableTypeName;

        private readonly List<UserDefinedTableTypeColumnDetail> _columnDetails;

        public List<string> GetAlterQuery()
        {
            var alterQuery = new List<string>
            {
                $"DROP TYPE [{_schemaName}].[{_tableTypeName}]",
                "GO"
            };

            alterQuery.AddRange(GetDefintion());

            return alterQuery;
        }

        public List<string> GetDefintion()
        {
            var definition = new List<string>
            {
                $"CREATE TYPE [{_schemaName}].[{_tableTypeName}] AS TABLE(",
                string.Join(",\n", _columnDetails.Select(c => "  " + c.GetAlterQuery())),
                ")",
                "GO"
            };
            
            return definition;
        }

    }
}

using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace SchemaSurgeon.ModifySqlFragments
{
    internal class ModifySprocDefinitionScriptGenerator : ModifySqlFragmentScriptGenerator<SprocIdentifier>
    {
        public ModifySprocDefinitionScriptGenerator(SqlConnection connection, CharacterDataTypeName newDataTypeName, Regex variableNamePattern) 
            : base(connection, newDataTypeName, variableNamePattern)
        {
        }

        protected override string SqlFragmentScriptFileName => "GetSprocsFromTable.sql";

        protected override string SqlFragmentSchemaColumn => "sproc_schema";

        protected override string SqlFragmentNameColumn => "sproc";

        protected override SprocIdentifier GetSqlFragmentIdentifier(string database, string sqlFragmentSchema, string sqlFragment)
        {
            return new SprocIdentifier(database, sqlFragmentSchema, sqlFragment);
        }
    }
}

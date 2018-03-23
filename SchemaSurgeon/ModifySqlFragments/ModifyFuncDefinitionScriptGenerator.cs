using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace SchemaSurgeon.ModifySqlFragments
{
    internal class ModifyFuncDefinitionScriptGenerator : ModifySqlFragmentScriptGenerator<FuncIdentifier>
    {
        public ModifyFuncDefinitionScriptGenerator(SqlConnection connection, CharacterDataTypeName newDataTypeName, Regex variableNamePattern) 
            : base(connection, newDataTypeName, variableNamePattern)
        {
        }

        protected override string SqlFragmentScriptFileName => "GetFuncsFromTable.sql";

        protected override string SqlFragmentSchemaColumn => "func_schema";

        protected override string SqlFragmentNameColumn => "func";

        protected override FuncIdentifier GetSqlFragmentIdentifier(string database, string sqlFragmentSchema, string sqlFragment)
        {
            return new FuncIdentifier(database, sqlFragmentSchema, sqlFragment);
        }
    }
}

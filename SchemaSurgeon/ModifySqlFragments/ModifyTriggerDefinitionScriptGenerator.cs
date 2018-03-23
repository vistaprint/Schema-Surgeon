using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace SchemaSurgeon.ModifySqlFragments
{
    internal class ModifyTriggerDefinitionScriptGenerator : ModifySqlFragmentScriptGenerator<TriggerIdentifier>
    {
        public ModifyTriggerDefinitionScriptGenerator(SqlConnection connection, CharacterDataTypeName newDatatype, Regex variableNamePattern)
            : base(connection, newDatatype, variableNamePattern)
        {
        }

        protected override string SqlFragmentScriptFileName => "GetTriggersFromTable.sql";

        protected override string SqlFragmentSchemaColumn => "trig_schema";

        protected override string SqlFragmentNameColumn => "trig";

        protected override TriggerIdentifier GetSqlFragmentIdentifier(string database, string sqlFragmentSchema, string sqlFragment)
        {
            return new TriggerIdentifier(database, sqlFragmentSchema, sqlFragment);
        }
    }
}

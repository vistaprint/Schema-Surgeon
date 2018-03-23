using Fclp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SchemaSurgeon.ModifyColumns;
using SchemaSurgeon.ModifySqlFragments;
using SchemaSurgeon.ReadSqlData;

namespace SchemaSurgeon
{
    public class Program
    {
        private class ApplicationArguments
        {
            public string ConnectionName { get; set; }
            public string NewDataType { get; set; }
            public List<string> ColumnSet { get; set; } 
            public string RegexColumn { get; set; }
            public string Schema { get; set; }
            public bool LogDetails { get; set; }
            public string IgnoreObjectsFileName { get; set; }
        }

        public static void Main(string[] args)
        {
            var parser = new FluentCommandLineParser<ApplicationArguments> {IsCaseSensitive = false};
            parser.Setup(arg => arg.ConnectionName).As('n', "connect").Required();
            parser.Setup(arg => arg.NewDataType).As('t', "type").Required();
            parser.Setup(arg => arg.Schema).As('s', "schema");
            parser.Setup(arg => arg.RegexColumn).As('r', "regex");
            parser.Setup(arg => arg.ColumnSet).As('c', "columns");
            parser.Setup(arg => arg.IgnoreObjectsFileName).As('i', "ignore").WithDescription(
                "File containing sprocs, functions, triggers to exclude");
            parser.Setup<bool>(arg => arg.LogDetails).As('l', "log").SetDefault(false).WithDescription("Log Details.");
            parser.SetupHelp("h", "help", "?").Callback(PrintUsage);

            var result = parser.Parse(args);

            if (result.HasErrors == false)
            {
                var newCharacterDataTypeName = new CharacterDataTypeName(parser.Object.NewDataType);

                if (parser.Object.RegexColumn != null && // find all columns that match a given pattern (e.g., .*customer.*id.*) in the database and create sql script to modify those columns
                    parser.Object.Schema != null)
                {
                    var connectionStringBuilder = BuildSqlConnectionString.GetConnectionString(parser.Object.ConnectionName);
                    string[] parts = parser.Object.Schema.Split('.');

                    if (parts.Length != 2)
                    {
                        throw new ArgumentException("spec", $"schema specification '{parser.Object.Schema}' was not in two-part database.schema format");
                    }
                    
                    var database = parts[0].Trim();
                    var schema = parts[1].Trim();
                    var regex = new Regex(parser.Object.RegexColumn, RegexOptions.IgnoreCase);
                    var fileName = parser.Object.IgnoreObjectsFileName;
                    List<DatabaseSchemaObjectIdentifier> ignoreObjectIds = null;
                    if (fileName != null)
                    {
                        if (!ReadObjectIds(fileName, database, schema, out ignoreObjectIds))
                        {
                            throw new ArgumentException($"Ignore File content was not in correct format: sproc:name or trigger:name or func:name");
                        }
                    }

                    // modify columns matching pattern and all other database objects that refer to them
                    var scriptGenerator = new ModifySchemaScriptGenerator(connectionStringBuilder, newCharacterDataTypeName);
                    scriptGenerator.GenerateScriptToAlterSchemaObjects(regex, database, schema, parser.Object.LogDetails, ignoreObjectIds); 
                }
                else if (parser.Object.ColumnSet != null)
                {
                    var connectionStringBuilder = BuildSqlConnectionString.GetConnectionString(parser.Object.ConnectionName);
                    var columns = parser.Object.ColumnSet.Select(c => new ColumnIdentifier(c));

                    // modify user-specified columns and all other columns containing foreign key references to these columns
                    var scriptGenerator = new ModifySchemaScriptGenerator(connectionStringBuilder, newCharacterDataTypeName); 
                    scriptGenerator.GenerateScriptToAlterColumns(columns);
                }
                else if (result.HelpCalled == false)
                {
                    PrintUsage();
                }
            }
            else if (result.EmptyArgs)
            {
                PrintUsage();
            }
            else 
            {
                Console.WriteLine(result.ErrorText);
            }
        }

        static bool ReadObjectIds(string fileName, string database, string schema, out List<DatabaseSchemaObjectIdentifier> ignoreObjectIdentifiers )
        {
            ignoreObjectIdentifiers = new List<DatabaseSchemaObjectIdentifier>();
            var objects = File.ReadAllLines(fileName);

            foreach (string s in objects)
            {
                string[] parts = s.Split(':');
                if (parts.Length != 2)
                {
                    return false;
                }

                if (parts[0] == "sproc")
                {
                    ignoreObjectIdentifiers.Add(new SprocIdentifier(database, schema, parts[1]));
                }
                else if (parts[0] == "func")
                {
                    ignoreObjectIdentifiers.Add(new FuncIdentifier(database, schema, parts[1]));
                }
                else if (parts[0] == "trigger")
                {
                    ignoreObjectIdentifiers.Add(new TriggerIdentifier(database, schema, parts[1]));
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("USAGES:");
            Console.WriteLine(
                "#1: SchemaSurgeon.exe -n <connection-name> -t <new-data-type> -c <initial-column> [<another-initial-column> <another-initial-column> ...] -l");
            Console.WriteLine(
                "   column specifications must be in four-part <database>.<schema>.<table>.<column> format.");
            Console.WriteLine("#2: SchemaSurgeon.exe -n <connection-name> -t <new-data-type> -s <database>.<schema> -r <column name regex> -l");
            Console.WriteLine("   schema specification must be in two-part <database>.<schema> format.");
            // Example: -n sandbox -t varchar(255) -s customer_db.dbo -r ".*customer.*id.*" -l
        }


        /*
         * Some sp_help... procedures return a comma separated list as part of their response
         * This function inspects the given rows, and if in the supplied csl, there is a match with *any* matchingItems
         * That row will be included in the returned stucture
         */
        private static IEnumerable<Dictionary<string, object>> GetRelevantIndexes(
            IEnumerable<Dictionary<string, object>> rows, string cslColumnName, IList<string> matchingItems)
        {
            var relevantRows = new List<Dictionary<string, object>>();

            foreach (var row in rows)
            {
                var items = ((string)row[cslColumnName]).Split(',').Select(x => x.Trim());

                if (items.Intersect(matchingItems).Any())
                {
                    relevantRows.Add(row);
                }
            }

            return relevantRows;
        }

        #region  C# data conversion

        private static List<Dictionary<string, object>> QueryResultToDictionary(IDataReader data)
        {
            var table = new List<Dictionary<string, object>>();
            while (data.Read())
            {
                table.Add(Enumerable.Range(0, data.FieldCount).ToDictionary(data.GetName, data.GetValue));
            }

            return table;
        }

        private static void PrintQueryResult(IEnumerable<Dictionary<string, object>> queryResults)
        {
            foreach (var row in queryResults)
            {
                var rowResult = "{" + string.Join(",", row.Select(col => col.Key + " = " + col.Value).ToArray()) + "}";
                Debug.WriteLine(rowResult);
            }
        }

        #endregion
    }
}

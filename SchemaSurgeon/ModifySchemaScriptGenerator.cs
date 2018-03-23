using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SchemaSurgeon.ModifyColumns;
using SchemaSurgeon.ModifySqlFragments;
using SchemaSurgeon.ModifyUserDefinedTableTypes;

namespace SchemaSurgeon
{
    class ModifySchemaScriptGenerator
    {
        protected SqlConnectionStringBuilder ConnectionStringBuilder { get; }
        protected CharacterDataTypeName NewDataTypeName { get; }

        public ModifySchemaScriptGenerator(SqlConnectionStringBuilder connectionStringBuilder, CharacterDataTypeName newDatatype)
        {
            ConnectionStringBuilder = connectionStringBuilder;
            NewDataTypeName = newDatatype;
        }

        /// <summary>
        /// Create sql scripts for modifying columns that match the given pattern and all other columns containing foreign key references and
        /// all sprocs, triggers, functions, user defined table types that contain references to the modified columns
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="database"></param>
        /// <param name="schema"></param>
        /// <param name="logDetails"></param>
        /// <param name="ignoreObjectIdentifiers"></param>
        public void GenerateScriptToAlterSchemaObjects(Regex regex, string database, string schema, bool logDetails = false, 
                                                       IEnumerable<DatabaseSchemaObjectIdentifier> ignoreObjectIdentifiers = null)
        {
            ConnectionStringBuilder.InitialCatalog = database;

            using (var connection = new SqlConnection(ConnectionStringBuilder.ToString()))
            {
                connection.Open();
                
                var generatedQueries = new GeneratedQueries { Database = database };

                Console.WriteLine($"Creating scripts for database: {database}");
                var visitedColumnSet = new HashSet<ColumnIdentifier>();

                ConnectionStringBuilder.InitialCatalog = database;
                var modifyColumnSchema = new ModifyColumnSchemaScriptGenerator(connection, NewDataTypeName);
                generatedQueries.AlterTableQueries.AddRange(modifyColumnSchema.GenerateScript(regex, schema, ref visitedColumnSet));

                if (regex != null) // process sprocs, functions, and triggers
                {
                    Console.WriteLine("Analyzing User Defined Table Types");
                    var modifyUserDefinedTableType = new ModifyUserDefinedTableTypeScriptGenerator(connection, NewDataTypeName, regex);
                    var userDefinedTableTypes = modifyUserDefinedTableType.GenerateScript();
                    generatedQueries.AlterUserDefinedTableTypeQueries.AddRange(userDefinedTableTypes);
                    var ignoreObjectIdList = ignoreObjectIdentifiers?.ToList();

                    var excludeSprocs = ignoreObjectIdList?.Where(id => id is SprocIdentifier).Cast<SprocIdentifier>();
                    var excludeFuncs = ignoreObjectIdList?.Where(id => id is FuncIdentifier).Cast<FuncIdentifier>();
                    var excludeTriggers = ignoreObjectIdList?.Where(id => id is TriggerIdentifier).Cast<TriggerIdentifier>();

                    Console.WriteLine("Analyzing Stored Procedures");
                    var modifySprocDefinition = new ModifySprocDefinitionScriptGenerator(connection, NewDataTypeName, regex);
                    generatedQueries.AlterSprocQueries.AddRange(modifySprocDefinition.GenerateScript(visitedColumnSet, excludeSprocs));

                    Console.WriteLine("Analyzing Functions");
                    var modifyFuncDefinition = new ModifyFuncDefinitionScriptGenerator(connection, NewDataTypeName, regex);
                    generatedQueries.AlterFuncQueries.AddRange(modifyFuncDefinition.GenerateScript(visitedColumnSet, excludeFuncs));

                    Console.WriteLine("Analyzing Triggers");
                    var modifyTriggerDefinition = new ModifyTriggerDefinitionScriptGenerator(connection, NewDataTypeName, regex);
                    generatedQueries.AlterTriggerQueries.AddRange(modifyTriggerDefinition.GenerateScript(visitedColumnSet, excludeTriggers));

                    if (logDetails)
                    {
                        CreateBeforeAndAfterFiles(database, "Sprocs", modifySprocDefinition.BeforeSqlText, modifySprocDefinition.AfterSqlText);
                        CreateBeforeAndAfterFiles(database, "Funcs", modifyFuncDefinition.BeforeSqlText, modifyFuncDefinition.AfterSqlText);
                        CreateBeforeAndAfterFiles(database, "Triggers", modifyTriggerDefinition.BeforeSqlText, modifyTriggerDefinition.AfterSqlText);
                        CreateBeforeAndAfterFiles(database, "TableType", modifyUserDefinedTableType.BeforeSqlText, modifyUserDefinedTableType.AfterSqlText);
                    }
                }

                generatedQueries.WriteOutputFiles();
            }
        }

        /// <summary>
        /// Create sql script for modifying given columns and all other columns containing foreign key references to these columns
        /// </summary>
        /// <param name="columns"></param>
        public void GenerateScriptToAlterColumns(IEnumerable<ColumnIdentifier> columns)
        {
            var columnsByDatabase = columns.GroupBy(col => col.Database);

            var generatedQueries = new GeneratedQueries();

            foreach (var columnDbGroup in columnsByDatabase)
            {
                string database = columnDbGroup.Key;
                generatedQueries.Database = database;

                Console.WriteLine($"Creating scripts for database: {database}");
    
                ConnectionStringBuilder.InitialCatalog = database;

                using (var connection = new SqlConnection(ConnectionStringBuilder.ToString()))
                {
                    connection.Open();
                    var modifyColumnSchema = new ModifyColumnSchemaScriptGenerator(connection, NewDataTypeName);
                    var queries = modifyColumnSchema.GenerateScript(columnDbGroup);

                    generatedQueries.AlterTableQueries.AddRange(queries);
                }
            }

            generatedQueries.WriteOutputFiles();
        }

        /// <summary>
        /// For debugging, write out the SQL before and after modification,
        /// so you can easily see (with a diff tool) what ScriptGenerator changed.
        /// </summary>
        private void CreateBeforeAndAfterFiles(string database, string fileNamePrefix, IEnumerable<string> beforeSqlText, IEnumerable<string> afterSqlText)
        {
            if (!Directory.Exists(database))
            {
                Directory.CreateDirectory(database);
            }

            string beforeFileName = Path.Combine(database, $"{fileNamePrefix}_Before.sql");
            File.WriteAllLines(beforeFileName, beforeSqlText);
            Console.WriteLine($"Created {beforeFileName}");

            string afterFileName = Path.Combine(database, $"{fileNamePrefix}_After.sql");
            File.WriteAllLines(afterFileName, afterSqlText);
            Console.WriteLine($"Created {afterFileName}");
        }
        
        private class GeneratedQueries
        {
            public string Database { private get; set; }
            public readonly List<string> AlterTableQueries = new List<string>();
            public readonly List<string> AlterSprocQueries = new List<string>();
            public readonly List<string> AlterFuncQueries = new List<string>();
            public readonly List<string> AlterTriggerQueries = new List<string>();
            public readonly List<string> AlterUserDefinedTableTypeQueries = new List<string>();

            public void WriteOutputFiles()
            {
                WriteQueriesToFile(AlterTableQueries, "AlterTables.sql");
                WriteQueriesToFile(AlterSprocQueries, "AlterSprocs.sql");
                WriteQueriesToFile(AlterFuncQueries, "AlterFuncs.sql");
                WriteQueriesToFile(AlterTriggerQueries, "AlterTriggers.sql");
                WriteQueriesToFile(AlterUserDefinedTableTypeQueries, "AlterUserDefinedTableTypes.sql");
            }

            private void WriteQueriesToFile(IEnumerable<string> queries, string fileName)
            {
                List<string> queriesList = queries.ToList();
                if (!queriesList.Any())
                {
                    return;
                }

                if (!Directory.Exists(Database))
                {
                    Directory.CreateDirectory(Database);
                }

                File.WriteAllLines(Path.Combine(Database, fileName), queriesList);
                Console.WriteLine($"Created {fileName}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SchemaSurgeon.ModifyColumns;
using SchemaSurgeon.ReadSqlData;

namespace SchemaSurgeon.ModifySqlFragments
{
    internal abstract class ModifySqlFragmentScriptGenerator<TSqlFragmentIdentifier>
        where TSqlFragmentIdentifier : DatabaseSchemaObjectIdentifier
    {
        protected SqlConnection Connection { get; }
        protected CharacterDataTypeName NewDataTypeName { get; }
        protected Regex VariableNamePattern { get; }

        private List<string> _beforeSqlText = new List<string>();
        private List<string> _afterSqlText = new List<string>();

        public List<string> BeforeSqlText => _beforeSqlText;
        public List<string> AfterSqlText => _afterSqlText;

        protected ModifySqlFragmentScriptGenerator(SqlConnection connection, CharacterDataTypeName newDatatype,
            Regex variableNamePattern)
        {
            Connection = connection;
            NewDataTypeName = newDatatype;
            VariableNamePattern = variableNamePattern;
        }

        protected abstract string SqlFragmentScriptFileName { get; }

        protected abstract string SqlFragmentSchemaColumn { get; }

        protected abstract string SqlFragmentNameColumn { get; }

        protected abstract TSqlFragmentIdentifier GetSqlFragmentIdentifier(string database, string sqlFragmentSchema, string sqlFragment);

        public IEnumerable<string> GenerateScript(HashSet<ColumnIdentifier> columns, IEnumerable<TSqlFragmentIdentifier> excludeSqlFragments = null)
        {
            var alterSqlFragmentQueries = new List<string>();
            var columnsByTable = columns.GroupBy(col => new {col.Schema, col.Table});
            var visitedSqlFragments = new HashSet<TSqlFragmentIdentifier>();
            var excludeSqlFragmentList = excludeSqlFragments?.ToList();

            foreach (var columnTableGroup in columnsByTable)
            {
                IEnumerable<TSqlFragmentIdentifier> identifiers = GetUniqueReferencingEntities(columnTableGroup.Key.Table, columnTableGroup.Key.Schema,
                    visitedSqlFragments, excludeSqlFragmentList);
                foreach (TSqlFragmentIdentifier identifier in identifiers)
                {
                    string sqlFragment = identifier.GetDefinition(Connection);
                    var analyzer = new SqlFragmentAnalyzer(identifier, sqlFragment, VariableNamePattern, NewDataTypeName);
                    // Process content
                    analyzer.AnalyseStatements();
                    List<string> alterSqlFragmentQuery = analyzer.GetAlterQuery().ToList();
                    if (alterSqlFragmentQuery.Any())
                    {
                        alterSqlFragmentQueries.AddRange(alterSqlFragmentQuery);
                        Console.WriteLine($"Creating query for altering: {identifier.Name}");
                        analyzer.UpdateDiffs(ref _beforeSqlText, ref _afterSqlText);
                    }
                }
            }

            if (alterSqlFragmentQueries.Any())
            {
                alterSqlFragmentQueries.Insert(0, $"USE {Connection.Database};");
            }

            return alterSqlFragmentQueries;
        }

        private IEnumerable<TSqlFragmentIdentifier> GetUniqueReferencingEntities(string table, string schema, HashSet<TSqlFragmentIdentifier> visitedSqlFragments, 
                                                                                    List<TSqlFragmentIdentifier> excludeSqlFragments = null)
        {
            string database = Connection.Database;

            using (SqlDataReader sqlFragmentReader = SelectSqlData.SelectSqlFragmentsFromTable(table, schema, SqlFragmentScriptFileName, Connection))
            {
                while (sqlFragmentReader.Read())
                {
                    var sqlFragmentSchema = sqlFragmentReader[SqlFragmentSchemaColumn] as string;
                    var sqlFragmentName = sqlFragmentReader[SqlFragmentNameColumn] as string;

                    TSqlFragmentIdentifier sqlFragmentIdentifier = GetSqlFragmentIdentifier(database, sqlFragmentSchema, sqlFragmentName);

                    if (!visitedSqlFragments.Contains(sqlFragmentIdentifier) && 
                        (excludeSqlFragments == null || !excludeSqlFragments.Contains(sqlFragmentIdentifier))
                        )
                    {
                        visitedSqlFragments.Add(sqlFragmentIdentifier);
                        yield return sqlFragmentIdentifier;
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using SchemaSurgeon.ModifyColumns.Constraints;
using SchemaSurgeon.ReadSqlData;

namespace SchemaSurgeon.ModifyColumns
{
	internal class ModifyColumnSchemaScriptGenerator
    {
        protected SqlConnection Connection { get; }
        protected CharacterDataTypeName NewDataTypeName { get; }

        public ModifyColumnSchemaScriptGenerator(SqlConnection connection, CharacterDataTypeName newDatatype)
        {
            Connection = connection;
            NewDataTypeName = newDatatype;
        }

        /// <summary>
        /// Generates script to modify all columns whose names match the given pattern and all other columns containing foreign key references to them
        /// </summary>
        /// <param name="columnNamePattern"></param>
        /// <param name="schema"></param>
        /// <param name="visitedColumnSet"></param>
        /// <returns></returns>
        public IEnumerable<string> GenerateScript(Regex columnNamePattern, string schema, ref HashSet<ColumnIdentifier> visitedColumnSet)
        {
            var columns = GetColumnsMatchingPattern(columnNamePattern, schema);
            var alterColumnQueries = GenerateScript(columns, ref visitedColumnSet);

            return alterColumnQueries;
        }

        /// <summary>
        /// Generates script to modify specified columns and other columns that contain foreign key references to them
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public IEnumerable<string> GenerateScript(IEnumerable<ColumnIdentifier> columns)
        {
            var visitedColumnSet = new HashSet<ColumnIdentifier>();
            var alterColumnQueries = GenerateScript(columns, ref visitedColumnSet);
           
            return alterColumnQueries;
        }

        private IEnumerable<string> GenerateScript(IEnumerable<ColumnIdentifier> columns, ref HashSet<ColumnIdentifier> visitedColumnSet)
        {
            var alterColumnQueries = new List<string>();

            foreach (var column in columns)
            {
                if (!visitedColumnSet.Contains(column)) // no duplicates
                {
                    var root = GetRootColumn(column);
                    var alterColumnTypeDetails = AlterColumnToDataType(root, null, visitedColumnSet);
                    alterColumnQueries.AddRange(AlterDetailsToQueryList(alterColumnTypeDetails));
                    foreach (var columnDetail in alterColumnTypeDetails.AffectedColumnDetails)
                    {
                        Console.WriteLine(
                            $"Creating query for altering Column: {columnDetail.ColumnName} in Table: {columnDetail.TableName}");
                    }
                }
            }

            if (alterColumnQueries.Any())
            {
                alterColumnQueries.Insert(0, $"USE {Connection.Database};");
            }

            return alterColumnQueries;
        }

        // Get all columns whose names match the given pattern
        private IEnumerable<ColumnIdentifier> GetColumnsMatchingPattern(Regex variableNamePattern, string schema)
        {
            using (var reader = SelectSqlData.SelectColumnsOfTypeString(Connection))
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        string table = reader["tab"] as string;
                        string column = reader["col"] as string;
                        int columnMaxLength = Convert.ToInt32(reader["col_max_length"]);

                        if (column != null && variableNamePattern.IsMatch(column) && columnMaxLength < NewDataTypeName.MaxDataSize)
                        {
                            yield return new ColumnIdentifier(Connection.Database, schema, table, column);
                        }
                    }
                }

                reader.Close();
            }
        }
    
        // Recursive function to get topmost column, i.e., column with no outbound foreign key reference in the given database
        private ColumnIdentifier GetRootColumn(ColumnIdentifier rootCol)
        {
            if (rootCol != null)
            {
                ColumnIdentifier root = GetRootColumn(GetOutboundForeignKey(rootCol));

                if (root != null)
                {
                    return root;
                }
            }

            return rootCol;
        }

        private AlterColumnTypeDetails AlterColumnToDataType(ColumnIdentifier columnIdentifier, string incomingForeignKey, ISet<ColumnIdentifier> visitedColumnSet)
        {
            // Alter the requested column

            var affectedColumnDetails = new List<ColumnDetail>
            {
                GetColumnDetails(columnIdentifier.Schema, columnIdentifier.Table,columnIdentifier.Column)
            };

            // Get all constraints referencing tableName.columnName
            var affectedConstraintDetails = GetConstraintDetails(columnIdentifier.Schema, columnIdentifier.Table, columnIdentifier.Column, incomingForeignKey).ToList();

            // Get all foreign keys referencing tableName.columnName
            var foreignKeys = affectedConstraintDetails.OfType<ForeignKeyDetail>().ToList();

            // Recursively get column and constraint details on each foreign key
            foreach (var foreignKey in foreignKeys)
            {
                var columnMap = foreignKey.Columns.First(column => column.TargetColumn == columnIdentifier.Column);
                var foreignKeySpec = new ColumnIdentifier(columnIdentifier.Database, columnIdentifier.Schema, foreignKey.Table, columnMap.SourceColumn);

                var alterColumnTypeDetails = AlterColumnToDataType(foreignKeySpec, foreignKey.Name, visitedColumnSet);

                // Mark each foreign key column as visited (post-order traversal)
                visitedColumnSet.Add(foreignKeySpec);

                affectedConstraintDetails.AddRange(alterColumnTypeDetails.AffectedConstraintDetails.Where(newCon => !affectedConstraintDetails.Any(oldCon => oldCon.Name == newCon.Name && oldCon.Table == newCon.Table)));
                affectedColumnDetails.AddRange(alterColumnTypeDetails.AffectedColumnDetails);
            }

            // Mark current column as visited (post-order traversal)
            visitedColumnSet.Add(columnIdentifier);

            // Sort by priority
            affectedConstraintDetails.Sort(
                (detail1, detail2) => Comparer<Int32>.Default.Compare(detail1.Priority, detail2.Priority));

            return new AlterColumnTypeDetails(affectedConstraintDetails, affectedColumnDetails);
        }

        private static IEnumerable<string> AlterDetailsToQueryList(AlterColumnTypeDetails details)
        {
            // Drop constraints in reverse order that we add them in
            var dropConstraintQueries = details.AffectedConstraintDetails.Reverse().Select(x => x.GetDropQuery());

            var alterColumnQueries = details.AffectedColumnDetails.Select(x => x.GetAlterQuery());
            var addConstraintQueries = details.AffectedConstraintDetails.Select(x => x.GetAddQuery());

            return dropConstraintQueries.Concat(alterColumnQueries).Concat(addConstraintQueries);
        }

        private ColumnDetail GetColumnDetails(string schemaName, string tableName, string columnName)
        {
            bool nullable = SelectSqlData.SelectColumnInfo(Connection, tableName, columnName);

            return new ColumnDetail(Connection.Database, schemaName, tableName, columnName, NewDataTypeName.ToString(), nullable);
        }

        private IEnumerable<ConstraintDetail> GetConstraintDetails(string schemaName, string tableName, string columnName, string inboundForeignKey)
        {
            string databaseName = Connection.Database;

            using (var constraintReader = SelectSqlData.SelectIndexesConstraintsAndStatistics(Connection, tableName, columnName))
            {
                while (constraintReader.Read())
                {
                    string constraintType = constraintReader["constraint_type"] as string;
                    string constraintName = constraintReader["index_name"] as string;
                    string constraintData = constraintReader["data"] as string;
                    bool disabled = (int)constraintReader["is_disabled"] > 0;
                    bool untrusted = (int)constraintReader["is_not_trusted"] > 0;

                    switch (constraintType)
                    {
                        case "FK_IN":
                            // it's a foreign key reference from this column
                            if (!StringComparer.OrdinalIgnoreCase.Equals(constraintName, inboundForeignKey))
                            {
                                throw new Exception(
                                    $"Column {databaseName}.{schemaName}.{tableName}.{columnName} is referenced by foreign key {constraintName} which comes from column {constraintData} - recursion should start from at least there.");
                            }
                            break;
                        case null:
                            // it's an index
                            yield return new IndexDetail(SelectSqlData.GetIndex(schemaName, tableName, constraintName, disabled, Connection));
                            break;
                        case "PK":
                            // it's a primary key constraint
                            yield return GetPrimaryKeyDetails(schemaName, tableName, constraintName, disabled);
                            break;
                        case "UQ":
                            // it's a unique constraint
                            yield return GetUniqueConstraintDetails(schemaName, tableName, constraintName, disabled);
                            break;
                        case "D":
                            // it's a default constraint
                            yield return GetDefaultConstraintDetails(constraintName, schemaName, tableName, columnName, constraintData);
                            break;
                        case "C":
                            // it's a check constraint
                            yield return GetCheckConstraintDetails(constraintName, schemaName, tableName, columnName, constraintData, untrusted, disabled);
                            break;
                        case "FK":
                            // it's a foreign key reference into this column
                            yield return SelectSqlData.GetForeignKeyDetails(schemaName, tableName, constraintName, untrusted, disabled, Connection);
                            break;
                        case "STAT":
                            // it's a statistics object on this column
                            yield return SelectSqlData.GetStatisticsDetails(schemaName, tableName, constraintName, Connection);
                            break;
                    }
                }

                yield break;
            }
        }

        private ColumnIdentifier GetOutboundForeignKey(ColumnIdentifier columnIdentifier)
        {
            var constraintReader = SelectSqlData.SelectOutboundForeignKey(Connection, columnIdentifier.Table, columnIdentifier.Column);
            if (constraintReader.Read())
            {
                string foreignTable = constraintReader["foreign_table"] as string;
                string foreignColumn = constraintReader["foreign_column"] as string;
                return new ColumnIdentifier(columnIdentifier.Database, columnIdentifier.Schema, foreignTable, foreignColumn);
            }

            return null;
        }

        private CheckDetail GetCheckConstraintDetails(string constraintName, string schemaName, string tableName, string columnName, string data, bool untrusted, bool isDisabled)
        {
            return new CheckDetail(constraintName, Connection.Database, schemaName, tableName, columnName, data, untrusted, isDisabled);
        }

        private DefaultDetail GetDefaultConstraintDetails(string constraintName, string schemaName, string tableName, string columnName, string data)
        {
            return new DefaultDetail(constraintName, Connection.Database, schemaName, tableName, columnName, data);
        }

        private UniqueKeyDetail GetUniqueConstraintDetails(string schemaName, string tableName, string constraintName, bool disabled)
        {
            return new UniqueKeyDetail(SelectSqlData.GetIndex(schemaName, tableName, constraintName, disabled, Connection));
        }

        private PrimaryKeyDetail GetPrimaryKeyDetails(string schemaName, string tableName, string constraintName, bool disabled)
        {
            return new PrimaryKeyDetail(SelectSqlData.GetIndex(schemaName, tableName, constraintName, disabled, Connection));
        }
    }
}

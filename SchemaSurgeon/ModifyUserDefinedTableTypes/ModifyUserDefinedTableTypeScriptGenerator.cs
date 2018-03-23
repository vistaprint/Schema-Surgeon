using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using SchemaSurgeon.ModifyColumns;
using SchemaSurgeon.ModifySqlFragments;
using SchemaSurgeon.ReadSqlData;

namespace SchemaSurgeon.ModifyUserDefinedTableTypes
{
    class ModifyUserDefinedTableTypeScriptGenerator
    {
        protected SqlConnection Connection { get; }
        protected CharacterDataTypeName NewDataTypeName { get; }
        protected Regex VariableNamePattern { get; }

        private List<string> _beforeSqlText = new List<string>();
        private List<string> _afterSqlText = new List<string>();

        public List<string> BeforeSqlText => _beforeSqlText;
        public List<string> AfterSqlText => _afterSqlText;
        
        public ModifyUserDefinedTableTypeScriptGenerator(SqlConnection connection, CharacterDataTypeName newDatatype, Regex variableNamePattern)
        {
            Connection = connection;
            NewDataTypeName = newDatatype;
            VariableNamePattern = variableNamePattern;
        }

        public IEnumerable<string> GenerateScript()
        {
            var query = new List<string>();
            _beforeSqlText = new List<string>();
            _afterSqlText = new List<string>();
            var columnIdentifiers = GetColumnsInUserDefinedTableTypesMatchingPattern();

            foreach (var columnIdentifier in columnIdentifiers)
            {
                query.AddRange(GetAlterQueryForUserDefinedTableType(columnIdentifier));
            }

            if (query.Any())
            {
                query.Insert(0, $"USE {Connection.Database};");
            }

            return query;
        }

        // Get all columns in user-defined table types that match the given pattern
        private IEnumerable<ColumnIdentifier> GetColumnsInUserDefinedTableTypesMatchingPattern()
        {
            using (var reader = SelectSqlData.SelectUserDefinedTableTypes(Connection))
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        string name = reader["name"] as string;
                        string column = reader["col"] as string;
                        string schema = reader["schema_name"] as string;

                        if (column != null && VariableNamePattern.IsMatch(column))
                        {
                            yield return new ColumnIdentifier(Connection.Database, schema, name, column);
                        }
                    }
                }

                reader.Close();
            }
        }

        // Get query for altering user defined table type
        private IEnumerable<string> GetAlterQueryForUserDefinedTableType(ColumnIdentifier columnIdentifier)
        {
            var alterQuery = new List<string> { "BEGIN TRAN" };
            string typeName = columnIdentifier.Table;
            string typeSchema = columnIdentifier.Schema;

            var sprocs = GetSprocsWithUserDefinedTableType(typeName, typeSchema).ToList();
            var funcs = GetFuncsWithUserDefinedTableType(typeName, typeSchema).ToList();
            var triggers = GetTriggersWithUserDefinedTableType(typeName, typeSchema).ToList();

            // Drop functions and sprocs
            alterQuery.AddRange(sprocs.Select(sproc => sproc.GetDropQuery()));
            alterQuery.AddRange(funcs.Select(func => func.GetDropQuery()));
            alterQuery.AddRange(triggers.Select(trigger => trigger.GetDropQuery()));

            List<UserDefinedTableTypeColumnDetail> existingColumnDetails;
            var updatedColumnDetails = GetUpdatedColumnDetailsInUserDefinedTableType(typeName, typeSchema, columnIdentifier.Column, out existingColumnDetails);

            var updatedTableTypeDetails = new UserDefinedTableTypeDetail(typeSchema, typeName, updatedColumnDetails.ToList());
            var existingTableTypeDetails = new UserDefinedTableTypeDetail(typeSchema, typeName, existingColumnDetails.ToList());
            alterQuery.AddRange(updatedTableTypeDetails.GetAlterQuery());
            BeforeSqlText.AddRange(existingTableTypeDetails.GetDefintion());
            AfterSqlText.AddRange(updatedTableTypeDetails.GetDefintion());

            // Recreate functions and sprocs with original definitions
            foreach (var sproc in sprocs)
            {
                alterQuery.AddRange(GetCreateQuery(sproc.GetDefinition(Connection)));
            }

            foreach (var func in funcs)
            {
                alterQuery.AddRange(GetCreateQuery(func.GetDefinition(Connection)));
            }

            foreach (var trigger in triggers)
            {
                alterQuery.AddRange(GetCreateQuery(trigger.GetDefinition(Connection)));
            }

            alterQuery.Add("COMMIT TRAN");

            return alterQuery;
        }

        private IEnumerable<UserDefinedTableTypeColumnDetail> GetUpdatedColumnDetailsInUserDefinedTableType(
                string tableType, string tableTypeSchema, string columnToBeUpdated,out List<UserDefinedTableTypeColumnDetail> existingColumnDetails)
        {
            var updatedColumnDetails = new List<UserDefinedTableTypeColumnDetail>();
            existingColumnDetails = new List<UserDefinedTableTypeColumnDetail>();

            using (var reader = SelectSqlData.SelectColumnsInUserDefinedTableType(Connection, tableType, tableTypeSchema))
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var columnName = reader["column_name"] as string;

                        bool isNullable = Convert.ToBoolean(reader["is_nullable"]);

                        int? existingCharacterMaxLength;
                        var existingDataType = reader["data_type"] as string;
                        switch (existingDataType)
                        {
                            case "char":
                            case "varchar":
                                existingCharacterMaxLength = Convert.ToInt32(reader["character_maximum_length"]);
                                break;
                            case "int":
                                existingCharacterMaxLength = null; // size does not get specified in definition
                                break;
                            default:
                                throw new InvalidOperationException($"Error creating User-Defined Table Type definition. Can't handle type: {existingDataType}.");
                        }

                        string dataType;
                        int? characterMaxLength;

                        if (columnName == columnToBeUpdated &&
                            existingCharacterMaxLength != null &&
                            NewDataTypeName.MaxDataSize > existingCharacterMaxLength.Value)
                        {
                            dataType = NewDataTypeName.DataType;
                            characterMaxLength = NewDataTypeName.MaxDataSize;
                        }
                        else
                        {
                            dataType = existingDataType;
                            characterMaxLength = existingCharacterMaxLength;
                        }

                        existingColumnDetails.Add(new UserDefinedTableTypeColumnDetail(Connection.Database, tableType, tableTypeSchema, columnName, existingDataType, isNullable, existingCharacterMaxLength));
                        updatedColumnDetails.Add(new UserDefinedTableTypeColumnDetail(Connection.Database, tableType, tableTypeSchema, columnName, dataType, isNullable, characterMaxLength));
                    }
                }
            }

            return updatedColumnDetails;
        }
        
        private IEnumerable<SprocIdentifier> GetSprocsWithUserDefinedTableType(string tableType, string tableTypeSchema)
        {
            using (var reader = SelectSqlData.SelectSprocsWithUserDefinedTableType(Connection, tableType, tableTypeSchema))
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        string sproc = reader["sproc_name"] as string;
                        string schema = reader["sproc_schema"] as string;

                        yield return new SprocIdentifier(Connection.Database, schema, sproc);
                    }
                }

                reader.Close();
            }
        }

        private IEnumerable<FuncIdentifier> GetFuncsWithUserDefinedTableType(string tableType, string tableTypeSchema)
        {
            using (var reader = SelectSqlData.SelectFuncsWithUserDefinedTableType(Connection, tableType, tableTypeSchema))
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        string func = reader["func_name"] as string;
                        string schema = reader["func_schema"] as string;

                        yield return new FuncIdentifier(Connection.Database, schema, func);
                    }
                }

                reader.Close();
            }
        }

        private IEnumerable<TriggerIdentifier> GetTriggersWithUserDefinedTableType(string tableType, string tableTypeSchema)
        {
            using (var reader = SelectSqlData.SelectTriggersWithUserDefinedTableType(Connection, tableType, tableTypeSchema))
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        string trigger = reader["trigger_name"] as string;
                        string schema = reader["trigger_schema"] as string;

                        yield return new TriggerIdentifier(Connection.Database, schema, trigger);
                    }
                }

                reader.Close();
            }
        }

        private static IEnumerable<string> GetCreateQuery(string definition)
        {
            if (definition == null) // cannot be created
            {
                return new List<string>();
            }

            var createQuery = new List<string>
            {
                "SET ANSI_NULLS ON",
                "GO",
                "SET QUOTED_IDENTIFIER ON",
                "GO",
                definition,
                "GO"
            };

            return createQuery;
        }
    }
}

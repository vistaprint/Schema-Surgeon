using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using SchemaSurgeon.ModifyColumns;
using SchemaSurgeon.ModifyColumns.Constraints;

namespace SchemaSurgeon.ReadSqlData
{
	internal static class SelectSqlData
    {
        public static bool SelectColumnInfo(SqlConnection connection, string tableName, string columnName)
        {
            var query = ReadSqlText("ColumnInfo.sql");

            var sqlCommand = new SqlCommand(query, connection);

            sqlCommand.Parameters.AddWithValue("@table_name", tableName);
            sqlCommand.Parameters.AddWithValue("@column_name", columnName);

            bool nullable = (bool)sqlCommand.ExecuteScalar();

            return nullable;
        }

        public static SqlDataReader SelectForeignKeys(SqlConnection connection)
        {
            var query = ReadSqlText("GetForeignKeys.sql");

            var sqlCommand = new SqlCommand(query, connection);

            return sqlCommand.ExecuteReader();
        }

        public static SqlDataReader SelectOutboundForeignKey(SqlConnection connection, string targetTable, string targetColumn)
        {
            var query = ReadSqlText("GetOutboundForeignKey.sql");

            var sqlCommand = new SqlCommand(query, connection);

            sqlCommand.Parameters.AddWithValue("@table_name", targetTable);
            sqlCommand.Parameters.AddWithValue("@column_name", targetColumn);

            return sqlCommand.ExecuteReader();
        }

        public static SqlDataReader SelectIndexesConstraintsAndStatistics(SqlConnection connection, string targetTable, string targetColumn)
        {
            var query = ReadSqlText("IndexesConstraintsAndStatistics.sql");

            var sqlCommand = new SqlCommand(query, connection);

            sqlCommand.Parameters.AddWithValue("@table_name", targetTable);
            sqlCommand.Parameters.AddWithValue("@column_name", targetColumn);

            return sqlCommand.ExecuteReader();
        }

        public static SqlDataReader SelectColumnsOfTypeString(SqlConnection connection)
        {
            var query = ReadSqlText("GetStringColumns.sql");

            var sqlCommand = new SqlCommand(query, connection);

            sqlCommand.Parameters.AddWithValue("@typeDesc", "USER_TABLE");

            return sqlCommand.ExecuteReader();
        }

        public static SqlDataReader SelectUserDefinedTableTypes(SqlConnection connection)
        {
            var query = ReadSqlText("GetStringColumnsInUserDefinedTableTypes.sql");

            var sqlCommand = new SqlCommand(query, connection);

            return sqlCommand.ExecuteReader();
        }

        public static SqlDataReader SelectSprocsWithUserDefinedTableType(SqlConnection connection, string tableType, string tableTypeSchema)
        {
            var query = ReadSqlText("GetSprocsWithUserDefinedTableType.sql");

            var sqlCommand = new SqlCommand(query, connection);

            sqlCommand.Parameters.AddWithValue("@user_defined_table_type_schema", tableTypeSchema);
            sqlCommand.Parameters.AddWithValue("@user_defined_table_type_name", tableType);

            return sqlCommand.ExecuteReader();
        }

        public static SqlDataReader SelectFuncsWithUserDefinedTableType(SqlConnection connection, string tableType, string tableTypeSchema)
        {
            var query = ReadSqlText("GetFuncsWithUserDefinedTableType.sql");

            var sqlCommand = new SqlCommand(query, connection);

            sqlCommand.Parameters.AddWithValue("@user_defined_table_type_schema", tableTypeSchema);
            sqlCommand.Parameters.AddWithValue("@user_defined_table_type_name", tableType);

            return sqlCommand.ExecuteReader();
        }

        public static SqlDataReader SelectTriggersWithUserDefinedTableType(SqlConnection connection, string tableType, string tableTypeSchema)
        {
            var query = ReadSqlText("GetTriggersWithUserDefinedTableType.sql");

            var sqlCommand = new SqlCommand(query, connection);

            sqlCommand.Parameters.AddWithValue("@user_defined_table_type_schema", tableTypeSchema);
            sqlCommand.Parameters.AddWithValue("@user_defined_table_type_name", tableType);

            return sqlCommand.ExecuteReader();
        }

        public static SqlDataReader SelectColumnsInUserDefinedTableType(SqlConnection connection, string tableType,
            string tableTypeSchema)
        {
            var query = ReadSqlText("GetColumnsInUserDefinedTableType.sql");

            var sqlCommand = new SqlCommand(query, connection);

            sqlCommand.Parameters.AddWithValue("@user_defined_table_type_schema", tableTypeSchema);
            sqlCommand.Parameters.AddWithValue("@user_defined_table_type_name", tableType);

            return sqlCommand.ExecuteReader();
        }

        public static StatisticsDetail GetStatisticsDetails(string schemaName, string tableName, string constraintName, SqlConnection connection)
        {
            var query = ReadSqlText("GetStatisticsColumns.sql");

            var sqlCommand = new SqlCommand(query, connection);

            sqlCommand.Parameters.AddWithValue("@stat_name", constraintName);
            sqlCommand.Parameters.AddWithValue("@table_name", tableName);

            using (var reader = sqlCommand.ExecuteReader())
            {
                var columns = new List<string>();

                while (reader.Read())
                {
                    var columnName = reader["column_name"] as string;
                    columns.Add(columnName);
                }

                return new StatisticsDetail(constraintName, connection.Database, schemaName, tableName, columns);
            }
        }

        public static ForeignKeyDetail GetForeignKeyDetails(string schemaName, string tableName, string constraintName, bool untrusted, bool disabled, SqlConnection connection)
        {
            var query = ReadSqlText("GetForeignKey.sql");

            var sqlCommand = new SqlCommand(query, connection);

            sqlCommand.Parameters.AddWithValue("@fk_name", constraintName);

            using (var reader = sqlCommand.ExecuteReader())
            {
                reader.Read();

                string targetTable = reader["name"] as string;
                string deleteAction = reader["delete_referential_action_desc"] as string;
                string updateAction = reader["update_referential_action_desc"] as string;

                List<ForeignKeyColumnMap> columns = new List<ForeignKeyColumnMap>();

                reader.NextResult();

                while (reader.Read())
                {
                    columns.Add(new ForeignKeyColumnMap(reader["col"] as string, reader["foreign_table"] as string, reader["foreign_col"] as string));
                }

                return new ForeignKeyDetail(constraintName, connection.Database, schemaName, tableName, targetTable, deleteAction, updateAction, columns, untrusted, disabled);
            }
        }

        public static IndexSpec GetIndex(string schemaName, string tableName, string constraintName, bool isDisabled, SqlConnection connection)
        {
            var query = ReadSqlText("GetIndexData.sql");

            var sqlCommand = new SqlCommand(query, connection);

            sqlCommand.Parameters.AddWithValue("@index_name", constraintName);
            sqlCommand.Parameters.AddWithValue("@table_name", tableName);

            using (var reader = sqlCommand.ExecuteReader())
            {
                reader.Read();

                //PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 99

                string index_type = reader["type_desc"] as string;

                Dictionary<string, string> indexOptions = new Dictionary<string, string>();

                indexOptions["PAD_INDEX"] = GetBooleanOption(reader, "is_padded");
                indexOptions["IGNORE_DUP_KEY"] = GetBooleanOption(reader, "ignore_dup_key");
                indexOptions["ALLOW_ROW_LOCKS"] = GetBooleanOption(reader, "allow_row_locks");
                indexOptions["ALLOW_PAGE_LOCKS"] = GetBooleanOption(reader, "allow_page_locks");

                int fillFactor = Convert.ToInt32(reader["fill_factor"]);
                if (fillFactor > 0)
                {
                    indexOptions["FILLFACTOR"] = fillFactor.ToString();
                }

                List<ColumnIndexInfo> columns = new List<ColumnIndexInfo>();

                reader.NextResult();

                while (reader.Read())
                {
                    columns.Add(new ColumnIndexInfo(reader["column_name"] as string, (bool)reader["is_descending_key"], (bool)reader["is_included_column"]));
                }

                return new IndexSpec(constraintName, connection.Database, schemaName, tableName, index_type, indexOptions, columns, isDisabled);
            }
        }
        
        public static SqlDataReader SelectSqlFragmentsFromTable(string table, string schema, string sqlFragmentScriptFileName, SqlConnection connection)
        {
            string query = ReadSqlText(sqlFragmentScriptFileName);

            var sqlCommand = new SqlCommand(query, connection);

            sqlCommand.Parameters.AddWithValue("@schema", schema);
            sqlCommand.Parameters.AddWithValue("@table", table);

            return sqlCommand.ExecuteReader();
        }

        private static string GetBooleanOption(SqlDataReader reader, string key)
        {
            return (bool)reader[key] ? "ON" : "OFF";
        }

        private static string ReadSqlText(string fileName)
        {
            string path = "ReadSqlData/Scripts/";
            var query = File.ReadAllText(path + fileName);

            return query;
        }
    }
}

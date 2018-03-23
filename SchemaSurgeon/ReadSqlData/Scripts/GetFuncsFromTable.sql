SELECT source_funcs.name func, SCHEMA_NAME(source_funcs.SCHEMA_ID) func_schema
FROM sys.sql_expression_dependencies dependencies
	INNER JOIN sys.objects source_funcs
		ON dependencies.referencing_id = source_funcs.[object_id]
	INNER JOIN sys.objects dest_tables
		ON dependencies.referenced_id = dest_tables.[object_id]
WHERE (SCHEMA_NAME(dest_tables.SCHEMA_ID) = @schema AND referenced_entity_name = @table)
	AND (source_funcs.type_desc = 'SQL_SCALAR_FUNCTION' OR source_funcs.type_desc = 'SQL_TABLE_VALUED_FUNCTION')
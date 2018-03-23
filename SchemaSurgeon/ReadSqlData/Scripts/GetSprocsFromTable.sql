SELECT source_sprocs.name sproc, SCHEMA_NAME(source_sprocs.SCHEMA_ID) sproc_schema
FROM sys.sql_expression_dependencies dependencies
	INNER JOIN sys.objects source_sprocs
		ON dependencies.referencing_id = source_sprocs.[object_id]
	INNER JOIN sys.objects dest_tables
		ON dependencies.referenced_id = dest_tables.[object_id]
WHERE (SCHEMA_NAME(dest_tables.SCHEMA_ID) = @schema AND referenced_entity_name = @table)
	AND source_sprocs.type_desc = 'SQL_STORED_PROCEDURE'

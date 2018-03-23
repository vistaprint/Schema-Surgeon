SELECT source_triggers.name trig, SCHEMA_NAME(source_triggers.SCHEMA_ID) trig_schema
FROM sys.sql_expression_dependencies dependencies
	INNER JOIN sys.objects source_triggers
		ON dependencies.referencing_id = source_triggers.[object_id]
	INNER JOIN sys.objects dest_tables
		ON dependencies.referenced_id = dest_tables.[object_id]
WHERE (SCHEMA_NAME(dest_tables.SCHEMA_ID) = @schema AND referenced_entity_name = @table)
	AND source_triggers.type_desc = 'SQL_TRIGGER'
SELECT c.name as column_name, t.name as data_type, c.max_length as character_maximum_length, c.is_nullable
FROM sys.table_types tt
	INNER JOIN sys.columns c
		ON tt.type_table_object_id = c.object_id
	INNER JOIN sys.types t
		ON c.user_type_id = t.user_type_id
WHERE tt.name = @user_defined_table_type_name
	AND tt.schema_id = (
		SELECT TOP 1 schema_id
		FROM sys.schemas
		WHERE name = @user_defined_table_type_schema
	)
ORDER BY c.column_id

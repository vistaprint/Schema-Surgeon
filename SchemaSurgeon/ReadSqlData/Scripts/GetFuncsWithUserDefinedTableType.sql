SELECT OBJECT_NAME(modules.object_id) func_name, SCHEMA_NAME(o.SCHEMA_ID) func_schema
FROM sys.sql_expression_dependencies dependencies
	INNER JOIN sys.sql_modules modules 
		ON modules.object_id = dependencies.referencing_id
	INNER JOIN sys.objects o 
		ON o.object_id = modules.object_id
	INNER JOIN sys.types types 
		ON types.user_type_id= referenced_id
WHERE referenced_entity_name = @user_defined_table_type_name
	AND SCHEMA_NAME(types.schema_id) = @user_defined_table_type_schema
	AND (O.type_desc = 'SQL_SCALAR_FUNCTION' OR O.type_desc = 'SQL_TABLE_VALUED_FUNCTION')
SELECT type.name name, col.name col, USER_NAME(TYPE.schema_id) schema_name
FROM sys.table_types type
	INNER JOIN sys.columns col  INNER JOIN sys.objects t ON t.object_id = col.object_id
		ON TYPE.type_table_object_id = COL.object_id
	INNER JOIN sys.types ty 
		ON ty.user_type_id = col.user_type_id
WHERE TYPE.is_user_defined = 1 
	AND	(ty.name = 'char' OR ty.name = 'varchar')
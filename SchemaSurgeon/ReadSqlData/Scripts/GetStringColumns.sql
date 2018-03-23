SELECT t.name tab, c.name col, c.max_length col_max_length
FROM sys.columns c 
	INNER JOIN sys.objects t
		ON t.object_id = c.object_id
	INNER JOIN sys.types ty
		ON ty.user_type_id = c.user_type_id
AND t.type_desc = @typeDesc 
AND (ty.name = 'char' OR ty.name = 'varchar')
	
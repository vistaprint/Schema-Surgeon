SELECT tab.name, delete_referential_action_desc, update_referential_action_desc
FROM sys.foreign_keys fk
INNER JOIN sys.tables tab
ON fk.parent_object_id = tab.object_id
WHERE fk.name = @fk_name


;WITH column_schema AS 
( 
SELECT 
	table_id = tab.object_id,
	table_name = tab.name,
	column_name = col.name,
	column_id = col.column_id
 FROM sys.columns col
INNER JOIN sys.tables tab
ON col.object_id = tab.object_id
)
	SELECT col = home.column_name, foreign_table = away.table_name, foreign_col = away.column_name
	FROM sys.foreign_keys fk
	INNER JOIN sys.foreign_key_columns fkc
	ON fkc.constraint_object_id = fk.object_id
	INNER JOIN column_schema home on fkc.parent_object_id = home.table_id
	AND fkc.parent_column_id = home.column_id
	INNER JOIN column_schema away on fkc.referenced_object_id = away.table_id
	AND fkc.referenced_column_id = away.column_id
	WHERE fk.name = @fk_name
	ORDER BY constraint_column_id 
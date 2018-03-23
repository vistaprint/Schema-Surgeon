SELECT is_nullable = col.is_nullable
FROM sys.columns col
	INNER JOIN sys.tables tab
		ON col.object_id = tab.object_id
WHERE col.name = @column_name
	AND tab.name = @table_name
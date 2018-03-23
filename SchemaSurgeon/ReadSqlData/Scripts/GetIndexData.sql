SELECT [type]
      ,[type_desc]
      ,[is_unique]
      ,[data_space_id]
      ,[ignore_dup_key]
      ,[is_primary_key]
      ,[is_unique_constraint]
      ,[fill_factor]
      ,[is_padded]
      ,[is_disabled]
      ,[is_hypothetical]
      ,[allow_row_locks]
      ,[allow_page_locks]
      ,[has_filter]
      ,[filter_definition]
	FROM sys.indexes
WHERE name = @index_name

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
SELECT col.column_name, icol.is_descending_key, icol.is_included_column
	FROM sys.indexes ind
	INNER JOIN column_schema col
	ON ind.object_id = col.table_id
	INNER JOIN sys.index_columns icol
	ON icol.object_id = ind.object_id
	AND icol.index_id = ind.index_id 
	AND icol.column_id = col.column_id
WHERE ind.name = @index_name
AND col.table_name = @table_name
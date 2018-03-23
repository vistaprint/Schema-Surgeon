SELECT col.name as column_name
FROM sys.stats stat
	INNER JOIN sys.stats_columns stat_col
		ON stat.stats_id = stat_col.stats_id
			AND stat.object_id = stat_col.object_id
	INNER JOIN sys.columns col
		ON stat_col.column_id = col.column_id
			AND stat_col.object_id = col.object_id
	INNER JOIN sys.tables tab
		ON stat_col.object_id = tab.object_id
WHERE stat.name = @stat_name
	AND tab.name = @table_name
ORDER BY stat_col.stats_column_id

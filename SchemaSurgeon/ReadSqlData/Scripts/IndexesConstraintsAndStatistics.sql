WITH column_schema AS 
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
, index_schema AS
(
	SELECT col.*, icol.index_id, icol.index_column_id, index_name = ind.name, is_disabled = ind.is_disabled
	FROM sys.indexes ind
	INNER JOIN column_schema col
	ON ind.object_id = col.table_id
	INNER JOIN sys.index_columns icol
	ON icol.object_id = ind.object_id
	AND icol.index_id = ind.index_id 
	AND icol.column_id = col.column_id
)
, key_constraint_schema AS
(
	SELECT ind.*, constraint_type = con.type, data = NULL, is_not_trusted = 0
	FROM sys.key_constraints con
	RIGHT JOIN index_schema ind 
	ON ind.table_id = con.parent_object_id AND ind.index_id = con.unique_index_id
) 
, default_constraint_schema AS
(
	SELECT col.*, index_id = null, index_column_id = null, index_name = def.name, is_disabled = 0, constraint_type = 'D', data = def.definition, is_not_trusted = 0
	FROM sys.default_constraints def
	INNER JOIN column_schema col
	ON col.table_id = def.parent_object_id
	AND col.column_id = def.parent_column_id
)

, check_constraint_schema AS
(
	SELECT col.*, index_id = null, index_column_id = null, index_name = chk.name, chk.is_disabled, constraint_type = 'C', data = chk.definition, chk.is_not_trusted
	FROM sys.check_constraints chk
	INNER JOIN column_schema col
	ON col.table_id = chk.parent_object_id
	AND col.column_id = chk.parent_column_id
)

, fk_in_constraint_schema AS
(
	SELECT home.*, index_id = NULL, index_column_id = NULL, index_name = fk.name, fk.is_disabled, constraint_type = 'FK_IN', data = away.table_name + '.' + away.column_name, fk.is_not_trusted
	FROM sys.foreign_keys fk
	INNER JOIN sys.foreign_key_columns fkc
	ON fkc.constraint_object_id = fk.object_id
	INNER JOIN column_schema home on fkc.parent_object_id = home.table_id
	AND fkc.parent_column_id = home.column_id
	INNER JOIN column_schema away on fkc.referenced_object_id = away.table_id
	AND fkc.referenced_column_id = away.column_id

)
, fk_out_constraint_schema AS
(
	SELECT away.*, index_id = NULL, index_column_id = NULL, index_name = fk.name, fk.is_disabled, constraint_type = 'FK', data = home.table_name + '.' + home.column_name, fk.is_not_trusted
	FROM sys.foreign_keys fk
	INNER JOIN sys.foreign_key_columns fkc
	ON fkc.constraint_object_id = fk.object_id
	INNER JOIN column_schema home on fkc.parent_object_id = home.table_id
	AND fkc.parent_column_id = home.column_id
	INNER JOIN column_schema away on fkc.referenced_object_id = away.table_id
	AND fkc.referenced_column_id = away.column_id

)
, stat_schema AS
(
	SELECT col.*, index_id = statcol.stats_id, index_column_id = statcol.stats_column_id, index_name = stat.name, is_disabled = 0, constraint_type = 'STAT', data = NULL, is_not_trusted = 0
	FROM sys.stats stat
	INNER JOIN column_schema col
	ON stat.object_id = col.table_id
	INNER JOIN sys.stats_columns statcol
	ON statcol.object_id = stat.object_id
	AND statcol.stats_id = stat.stats_id
	AND statcol.column_id = col.column_id
	AND stat.user_created = 1
	AND INDEXPROPERTY(stat.OBJECT_ID,stat.NAME,'IsStatistics') = 1
)
, constraint_schema AS
(
	SELECT * FROM key_constraint_schema
	UNION ALL SELECT * FROM default_constraint_schema
	UNION ALL SELECT * FROM check_constraint_schema
	UNION ALL SELECT * FROM fk_in_constraint_schema
	UNION ALL SELECT * FROM fk_out_constraint_schema
	UNION ALL SELECT * FROM stat_schema
)
SELECT * FROM constraint_schema
WHERE table_name = @table_name
AND column_name = @column_name
ORDER BY table_id, index_id, index_column_id

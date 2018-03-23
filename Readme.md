SchemaSurgeon provides the ability to create scripts to alter the database. There are two ways to run this program:

1- Modifying data types of columns only

This mode allows you to modify the data type of explicitly named columns. 

SchemaSurgeon.exe -n <sandbox> -t <data_type> -c <database>.<schema>.<table_name>.<column_name> 

   Options: 
   
     -n 
	 Connection string spec (Must be specified in App.config with four fields: {data source}, {initial catalog}, {user Id}, {password})
	 
	 -t 
	 The new data type of the column(s) e.g., varchar(255). This data type must be bigger than the original data type so that existing data can be accommodated. For example char(3) can be updated to varchar(4) but not char(2). 
	 
	 -c 
	 Fully qualified column name(s) whose data type(s) need to be updated (any number of columns can be specified).

In this mode, the program generates a sql script (AlterTables.sql) that includes the specified columns as well as all other columns that contain foreign key references to them. The generated script can be run against a server to apply schema changes included in the script. This mode does not create scripts for modifying any other database objects. The specified columns can belong to different databases.

2- Modifying all database objects

This mode allows you to modify columns and all other database objects that contain references to those columns. You can specify a regular expression to define the pattern against which column names will be matched.

SchemaSurgeon.exe -n <sandbox> -t <data_type> -s <database>.<schema> -r "<regexp>" -log -i <ignore_databases_list.txt>

Options: 

     -n 
	 Connection string spec (Must be specified in App.config with four fields: {data source}, {initial catalog}, {user Id}, {password})
	 
	 -t 
	 The new data type of the column(s) e.g., varchar(255). This data type must be bigger than existing data type so that existing data can be accommodated. For example char(3) can be updated to char (4) but not char(2). 
	 
	 -s 
	 Fully qualified schema name
	 
	 -r
	 Regular expression that defines the pattern against which names of columns, functions, sprocs, triggers, etc. will be matched. E.g., ".*customer.*id.*"
	 
	 -log  
	 This is an optional parameter that can be specified to generate "Before" and "After" definitions of stored procedures, functions, triggers and user-defined table types that got modified. Any diff tool can be used to see the differences. 
	 For example, to view the "Before" and "After" definitions of stored procedures, use Funcs_Before.sql and Funcs_After.sql. All output files are generated in a folder with the name of the database that the tool was run on.
	 
	 -i 
	 This is an optional parameter to specify a text file that contains names of stored procedures, functions and triggers to be excluded. For example, certain procedures should not be recreated if they are deprecated or contain references to deprecated tables. The names of procedures, functions or triggers should be specified in the following format:

		sproc: sproc_name
		func: func_name
		trigger: trigger_name

In this mode, the program generates the following sql scripts that, when manually run against a server, alter all database objects that get affected. 

AlterTables.sql 
Contains a script to alter all columns whose names match the specified pattern (as well as all other columns that contain foreign key references - both inbound and outbound) to the desired data type.

AlterSprocs.sql 
Contains a script to alter all stored procedures that contain references to the tables whose columns are being modified and whose definitions contain variable declarations matching the given pattern. 

AlterFuncs.sql
Contains a script to alter all scalar-valued and table-valued functions that contain references to the tables whose columns are being modified and whose definitions contain variable declarations matching the given pattern. 
 
AlterTriggers.sql
Contains a script to alter triggers that contain references to the tables whose columns are being modified and whose definitions contain variable declarations matching the given pattern. 

AlterUserDefinedTableTypes.sql
Contains a script to alter user-defined table types that contain columns whose names match the given pattern. This script drops all stored procedures, functions and triggers that contain references to the table type being modified, drops and recreates the table type with updated column definition, and then recreates stored procedures, functions and triggers that were dropped. This sequence of operations occurs within a transaction.



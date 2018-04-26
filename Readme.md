# Schema Surgeon
> SchemaSurgeon provides the ability to amend the Microsoft SQL database schema by generating sql scripts that can be run against SQL server database.  


## Motivation
This tool was created out of a business need to expand the size of data fields in SQL database in order to accommodate larger size of data. For example, let's say that you have a database with several tables that contain columns for representing unique customer ids. Let's say that the data field that stores customer id is of type char(8). Now your customer base is growing and you need to expand the field to say varchar(50) to accommodate more customers. This is not a trivial task if you have hundreds of tables with such columns and foreign key references from other tables as well as references from other stored procedures, triggers, functions, etc. This tool provides the functionality to automate the process of finding and updating such fields to the desired data type.  
 

## Prerequisites
Microsoft .NET Framework 4.5.2 and MSBuild  
 

## Development setup

1. The easiest way to work with the code base is to install Visual Studio. You can also work with any other IDE provided you have .NET Framework 4.5.2 and MSBuild on your machine.
2. If you installed Visual Studio, you can now open the SchemaSurgeon.sln in the root of the source code and build using Visual Studio.
3. Open command prompt from /SchemaSurgeon/bin/Debug and run one of the commands decribed in the usage examples below. 

For example, let's say that you have a database called transactions with a table called customer_details that contains a column customer_id of type char(8). The connection string spec "sandbox" is defined in App.config. To update this column to use varchar(255), run the following command:

```SchemaSurgeon.exe -n sandbox -t varchar(255) -c transactions.dbo.customer_details.customer_id```
 

## Usage Examples

### Modifying data types of columns only

This mode allows you to modify the data type of explicitly named columns. 
Input: Connection, Database, Schema, Data type, Column names(s).
Output: AlterTables.sql

Command:
```SchemaSurgeon.exe -n *sandbox* -t *data_type* -c *database*.*schema*.*table_name*.*column_name*```

   Options: 
   
     -n 
	 Connection string spec (Must be specified in App.config with four fields: {data source}, {initial catalog}, {user Id}, {password})
	 
	 -t 
	 The new data type of the column(s) e.g., varchar(255). This data type must be bigger than the original data type so that existing data can be accommodated. For example char(3) can be updated to varchar(4) but not char(2). 
	 
	 -c 
	 Fully qualified column name(s) whose data type(s) need to be updated (any number of columns can be specified).

In this mode, the program generates a sql script (AlterTables.sql) that includes the specified columns as well as all other columns that contain foreign key references to them. The generated script can be run against a server to apply schema changes included in the script. This mode does not create scripts for modifying any other database objects. The specified columns can belong to different databases.

### Modifying all database objects

This mode allows you to modify columns and all other database objects that contain references to those columns. You can specify a regular expression to define the pattern against which column names will be matched.

Input: Connection, database, schema, data type, regular expression.
Output: AlterTables.sql, AlterSprocs.sql, AlterFuncs.sql, AlterTriggers.sql, AlterUserDefinedTableTypes.sql.

```SchemaSurgeon.exe -n *sandbox* -t *data_type* -s *database*.*schema* -r "*regexp*" -log -i *ignore_databases_list.txt*```

Options: 

     -n 
	 Connection string spec (Must be specified in App.config with four fields: {data source}, {initial catalog}, {user Id}, {password})
	 
	 -t 
	 The new data type of the column(s) e.g., varchar(255). This data type must be bigger than existing data type so that existing data can be accommodated. For example char(3) can be updated to char(4) but not char(2). 
	 
	 -s 
	 Fully qualified schema name
	 
	 -r
	 Regular expression that defines the pattern against which names of columns, and variable names in functions, sprocs, triggers, etc. will be matched. E.g., ".*customer.*id.*"
	 
	 -log  
	 This is an optional parameter that can be specified to generate "Before" and "After" definitions of stored procedures, functions, triggers and user-defined table types that got modified. Any diff tool can be used to see the differences. 
	 For example, to view the "Before" and "After" definitions of stored procedures, use Funcs_Before.sql and Funcs_After.sql. All output files are generated in a folder with the name of the database that the tool was run on.
	 
	 -i 
	 This is an optional parameter to specify a text file that contains names of stored procedures, functions and triggers to be excluded. For example, certain procedures should not be recreated if they are deprecated or contain references to deprecated tables. The names of procedures, functions or triggers should be specified in the following format:

		sproc: sproc_name
		func: func_name
		trigger: trigger_name

In this mode, the program generates the following sql scripts that, when manually run against a server, alter all database objects that get affected. 

* **AlterTables.sql** 

Contains a script to alter all columns whose names match the specified pattern (as well as all other columns that contain foreign key references - both inbound and outbound) to the desired data type.

* **AlterSprocs.sql** 

Contains a script to alter all stored procedures that contain references to the tables whose columns are being modified and whose definitions contain variable declarations matching the given pattern. 

* **AlterFuncs.sql**

Contains a script to alter all scalar-valued and table-valued functions that contain references to the tables whose columns are being modified and whose definitions contain variable declarations matching the given pattern. 
 
* **AlterTriggers.sql**

Contains a script to alter triggers that contain references to the tables whose columns are being modified and whose definitions contain variable declarations matching the given pattern. 

* **AlterUserDefinedTableTypes.sql**

Contains a script to alter user-defined table types that contain columns whose names match the given pattern. This script drops all stored procedures, functions and triggers that contain references to the table type being modified, drops and recreates the table type with updated column definition, and then recreates stored procedures, functions and triggers that were dropped. This sequence of operations occurs within a transaction.  
 
## Architecture

Please see [Architecture.md](Architecture.md) for details on underlying design.

## Known Bugs and Limitations
* Currently, only data fields of type string (char, varchar) can be expanded. However, the code can be easily extended to allow support for other data types in the future.
* The user defined table type column can be updated only if the table type contains columns of type char, varchar or int. Moreover, the script produced for altering user-defined table type assumes that only one column in the table type needs to be modified. 
* If any stored procedure, trigger or function that needs to be altered contains a bug in its definition (e.g., a reference to deprecated table), it is dropped but not re-created. An error or a warning is issued. Such errors can be eliminated by excluding the stored procedures with bad definitions. 

## Release History

* 0.0.1
   * The first proper release  


## Contributing

Please read [Contributing.md](Contributing.md) for details on the process for submitting pull requests to us as well as ideas for future contribution.  


## Contributors

* **Smita Narayan**
* **James Hart**
* [**Christopher Kwan**](https://github.com/chriskwan)
* **Vincent Del Toral** 
* **Kevin Campusano**


## License
Copyright [2018] Cimpress

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.




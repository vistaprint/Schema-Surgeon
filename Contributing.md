# Contributing

When contributing to this repository, please create a pull request with changes that should include updated README.md with details of changes to the interface if necessary. You may merge the Pull Request in once you have the sign-off of one developer.  
  
## Ideas for future expansion

### Support for data types other than string
Currently, data fields of type string (char, varchar) can be updated. The code can be extended to update other types of data fields (e.g., int to bigint). This will involve creating a class similar to CharacterDataTypeName in the root folder and updating its usages.

### Full support for user defined table type
Currently, user defined table type column can be updated only if the table type contains columns of type char, varchar or int. Otherwise, the user defined table type definition cannot be created. Support for other column types can be added in ModifyUserDefinedTableTypes/ModifyUserDefinedTableTypeScriptGenerator.



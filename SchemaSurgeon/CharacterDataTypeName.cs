using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SchemaSurgeon
{
    class CharacterDataTypeName
    {
        public string DataType { get; private set; }

        public int MaxDataSize => _maxDataSize;

        private int _maxDataSize;
        private string _dataTypeText;

        public CharacterDataTypeName(string dataType)
        {
            if (!Parse(dataType))
            {
                throw new ArgumentException("New Data Type was not specified correctly");
            }
        }

        public override string ToString()
        {
            return _dataTypeText; 
        }

        private bool Parse(string dataType)
        {
            Microsoft.SqlServer.TransactSql.ScriptDom.TSql130Parser sqlParser = new TSql130Parser(false);
            IList<ParseError> errors;
            TSqlFragment dataTypeFragment = sqlParser.Parse(new StringReader("declare @t " + dataType), out errors);

            if (!errors.Any())
            {
                var dataTypeDeclaration = dataTypeFragment as TSqlScript;
                var statement = dataTypeDeclaration?.Batches?[0]?.Statements?[0] as DeclareVariableStatement;
                var sqlDataType = statement?.Declarations?[0].DataType as SqlDataTypeReference;

                if (sqlDataType?.SqlDataTypeOption == SqlDataTypeOption.Char ||
                    sqlDataType?.SqlDataTypeOption == SqlDataTypeOption.VarChar)
                {
                    DataType = sqlDataType.SqlDataTypeOption.ToString().ToLower(); // only used by user defined table type - keep it lower case
                    if (sqlDataType.Parameters.Count > 0 && sqlDataType.Parameters[0].LiteralType == LiteralType.Integer)
                    {
                        _maxDataSize = Int32.Parse((sqlDataType.Parameters[0] as IntegerLiteral).Value);
                        _dataTypeText = dataType; 

                        return true;
                    }
                }
            }

            return false;
        }
    }
}

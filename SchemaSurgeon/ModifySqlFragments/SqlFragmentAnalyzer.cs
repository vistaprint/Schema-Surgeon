using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SchemaSurgeon.ModifySqlFragments
{
    internal class SqlFragmentAnalyzer
    {
        private readonly DatabaseSchemaObjectIdentifier _databaseSchemaObjectIdentifier;
        private readonly string _sqlFragment;
        private readonly Regex _variableNamePattern;
        private readonly AnalysisCollector _collector;
        private readonly CharacterDataTypeName _dataTypeName;
       
        public SqlFragmentAnalyzer(DatabaseSchemaObjectIdentifier databaseSchemaObjectIdentifier, string sqlFragment, Regex variableNamePattern, CharacterDataTypeName newDatatype)
        {
            _databaseSchemaObjectIdentifier = databaseSchemaObjectIdentifier;
            _sqlFragment = sqlFragment;
            _variableNamePattern = variableNamePattern;
            _dataTypeName = newDatatype;
            _collector = new AnalysisCollector(newDatatype);
        }

        public void AnalyseStatements()
        {
            Microsoft.SqlServer.TransactSql.ScriptDom.TSql130Parser parser = new TSql130Parser(false);

            IList<ParseError> errors;
            TSqlFragment fragment = parser.Parse(new StringReader(_sqlFragment), out errors);

            if (!errors.Any())
            {
                _collector.SetSqlFragment(fragment);
                fragment.Accept(new ColumnsMatchingPatternVisitor(_collector, _variableNamePattern, _dataTypeName));
            }
        }

        public string GetAddQuery()
        {
            return _collector.GetUpdatedSql();
        }

        public string GetDropQuery()
        {
            return _databaseSchemaObjectIdentifier.GetDropQuery();
        }

        public IEnumerable<string> GetAlterQuery()
        {
            string updatedSql = GetAddQuery();

            if (updatedSql == null) // no update required
            {
                return new List<string>();
            }

            var alterQuery = new List<string>
            {
                "GO",
                GetDropQuery(),
                "SET ANSI_NULLS ON",
                "GO",
                "SET QUOTED_IDENTIFIER ON",
                "GO",
                updatedSql,
                "GO"
            };

            return alterQuery;
        }

        public void UpdateDiffs(ref List<string> before, ref List<string> after)
        {
            string updatedSql = GetAddQuery();

            if (updatedSql != null) // no updates
            {
                before.Add(_sqlFragment);
                after.Add(updatedSql);
            }
        }

        // Visitor of sql fragment that visits column definitions and collects variable names that match the given pattern
        //
        private class ColumnsMatchingPatternVisitor : TSqlFragmentVisitor
        {
            private readonly AnalysisCollector _collector;
            private readonly Regex _variableNamePattern;
            private readonly CharacterDataTypeName _newDataTypeName;

            public ColumnsMatchingPatternVisitor(AnalysisCollector collector, Regex variableNamePattern, CharacterDataTypeName newDataTypeName)
            {
                _collector = collector;
                _variableNamePattern = variableNamePattern;
                _newDataTypeName = newDataTypeName;
            }

            public override void Visit(DeclareTableVariableStatement tableVariableStatement)
            {
                foreach (var columnDefinition in tableVariableStatement.Body.Definition.ColumnDefinitions)
                {
                    var value = columnDefinition.ColumnIdentifier.Value;
                    Analyze(value, columnDefinition.DataType as SqlDataTypeReference);
                }

                base.Visit(tableVariableStatement);
            }

            // This is getting called for create procedure parameters as well, so no need to visit ProcedureParameter node.
            //
            public override void Visit(DeclareVariableElement variableElement)
            {
                Analyze(variableElement.VariableName.Value, variableElement.DataType as SqlDataTypeReference);

                base.Visit(variableElement);
            }

            public override void Visit(CreateTableStatement createTableStatement)
            {
                foreach (var columnDefinition in createTableStatement.Definition.ColumnDefinitions)
                {
                    var value = columnDefinition.ColumnIdentifier.Value;
                    Analyze(value, columnDefinition.DataType as SqlDataTypeReference);

                }

                base.Visit(createTableStatement);
            }

            public override void Visit(TableDefinition tableDefinition)
            {
                foreach (var columnDefinition in tableDefinition.ColumnDefinitions)
                {
                    var value = columnDefinition.ColumnIdentifier.Value;
                    Analyze(value, columnDefinition.DataType as SqlDataTypeReference);
                }

                base.Visit(tableDefinition);
            }

            private void Analyze(string value, SqlDataTypeReference dataType)
            {
                if (dataType == null)
                {
                    return;
                }

                if (_variableNamePattern.IsMatch(value))
                {
                    if (dataType.SqlDataTypeOption == SqlDataTypeOption.Char ||
                         dataType.SqlDataTypeOption == SqlDataTypeOption.VarChar)
                    {
                        if (dataType.Parameters.Count > 0 && dataType.Parameters[0].LiteralType == LiteralType.Integer)
                        {
                            int size = Int32.Parse((dataType.Parameters[0] as IntegerLiteral).Value);

                            if (size < _newDataTypeName.MaxDataSize)
                            {
                                int startTokenIndex = dataType.FirstTokenIndex;
                                int endTokenIndex = dataType.LastTokenIndex;

                                _collector.CollectTokenToUpdate(startTokenIndex, endTokenIndex);
                            }
                        }
                    }
                }
            }
        }

        private class AnalysisCollector
        {
            private class TokenSequencePositionCollection
            {
                // dictionary containing start and end positions of tokens
                private readonly Dictionary<int, int> _tokens = new Dictionary<int, int>();

                public void AddTokenSequence(int startPosition, int endPosition)
                {
                    _tokens[startPosition] = endPosition;
                }
  
                public int GetTokenSequenceEndPosition(int startPosition)
                {
                    int endPosition = -1;

                    if (_tokens.ContainsKey(startPosition))
                    {
                        endPosition = _tokens[startPosition];
                    }

                    return endPosition;
                }

                public int Count()
                {
                    return _tokens.Count;
                }
            };

            private readonly TokenSequencePositionCollection _tokenSequencesToReplace = new TokenSequencePositionCollection();
            private TSqlFragment _sqlFragment;
            private readonly CharacterDataTypeName _newDataTypeName;

            public AnalysisCollector(CharacterDataTypeName newDataTypeName)
            {
                _newDataTypeName = newDataTypeName;
            }

            public void SetSqlFragment(TSqlFragment fragment)
            {
                _sqlFragment = fragment;
            }

            public void CollectTokenToUpdate(int startPosition, int endPosition)
            {
                _tokenSequencesToReplace.AddTokenSequence(startPosition, endPosition);
            }

            public string GetUpdatedSql()
            {
                if (_tokenSequencesToReplace.Count() == 0 || _sqlFragment == null)
                {
                    return null;
                }

                var builder = new StringBuilder();
                var allTokens = _sqlFragment.ScriptTokenStream;

                for (int position = 0; position < allTokens.Count; position++)
                {
                    var token = allTokens[position];
                    int endPosition = _tokenSequencesToReplace.GetTokenSequenceEndPosition(position);

                    if (endPosition == -1) // not found
                    {
                        builder.Append(token.Text);
                    }
                    else // token found - need to replace with the new datatype and update loop index to end position
                    {
                        builder.Append(_newDataTypeName);
                        position = endPosition;
                    }
                }

                return builder.ToString();
            }
        }
    }
}

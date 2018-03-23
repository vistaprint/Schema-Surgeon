using System;
using System.Data.SqlClient;
using System.Text;

namespace SchemaSurgeon.ModifySqlFragments
{
    public abstract class DatabaseSchemaObjectIdentifier
    {
        public string Database { get; }
        public string Schema { get; }
        public string Name { get; }

        protected DatabaseSchemaObjectIdentifier(string database, string schema, string name)
        {
            Database = database;
            Schema = schema;
            Name = name;
        }

        public abstract string GetDropQuery();

        public string GetDefinition(SqlConnection connection)
        {
            var helpSproc = "sp_helptext";
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = helpSproc;
                cmd.Parameters.AddWithValue("@objname", Schema + '.' + Name);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;

                using (var rdr = cmd.ExecuteReader())
                {
                    StringBuilder builder = new StringBuilder();

                    while (rdr.Read())
                    {
                        builder.Append(rdr["Text"]);
                    }

                    return builder.ToString();
                }
            }
        }

        public override bool Equals(Object obj)
        {
            var toIdentifier = obj as DatabaseSchemaObjectIdentifier;

            if (toIdentifier == null)
            {
                return false;
            }

            return (Database == toIdentifier.Database && Schema == toIdentifier.Schema &&
                    Name == toIdentifier.Name);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Database ?? string.Empty).GetHashCode() + (Schema ?? string.Empty).GetHashCode() +
                       (Name ?? string.Empty).GetHashCode();

            }
        }
    }
}

using System.Configuration;
using System.Data.SqlClient;

namespace SchemaSurgeon.ReadSqlData
{
    public static class BuildSqlConnectionString
    {
        public static SqlConnectionStringBuilder GetConnectionString(string environment)
        {
            var dataSource = ConfigurationManager.AppSettings[environment + ".data_source"];
            var initialCatalog = ConfigurationManager.AppSettings[environment + ".initial_catalog"];
            var userId = ConfigurationManager.AppSettings[environment + ".user_id"];
            var password = ConfigurationManager.AppSettings[environment + ".password"];

            var connectionString = 
                $"Data Source={dataSource};Initial Catalog={initialCatalog};User ID={userId};Password={password};MultipleActiveResultSets=true;";

            return new SqlConnectionStringBuilder(connectionString);
        }
    }
}

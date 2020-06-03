namespace Slice.ServerApp.Infrastructure
{
    public class SqlServerConnectionString : IConnectionString
    {
        public string ConnectionString { get; }
        public SqlServerConnectionString(string sqlServerConnectionString)
        {
            ConnectionString = sqlServerConnectionString;
        }       
    }
}

using Microsoft.Data.SqlClient;

namespace CollectionManager.Api.Data;

public sealed class SqlConnectionFactory(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("CollectionDb")
        ?? throw new InvalidOperationException("Missing CollectionDb connection string.");

    public SqlConnection Create() => new(_connectionString);
}


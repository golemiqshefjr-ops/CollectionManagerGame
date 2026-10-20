using Npgsql;

namespace CollectionManager.Api.Data;

public sealed class PostgresConnectionFactory(IConfiguration configuration)
{
    private readonly string _connectionString = BuildConnectionString(configuration.GetConnectionString("CollectionDb")
        ?? throw new InvalidOperationException("Missing CollectionDb connection string."));

    public NpgsqlConnection Create() => new(_connectionString);

    private static string BuildConnectionString(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("postgres" or "postgresql"))
            return value;

        var credentials = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : "",
            SslMode = SslMode.Require
        };
        return builder.ConnectionString;
    }
}

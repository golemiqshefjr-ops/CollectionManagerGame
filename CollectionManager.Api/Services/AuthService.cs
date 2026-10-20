using System.Security.Cryptography;
using System.Text;
using CollectionManager.Api.Auth;
using CollectionManager.Api.Data;
using Npgsql;

namespace CollectionManager.Api.Services;

public sealed class AuthService(PostgresConnectionFactory factory, TokenService tokens)
{
    public async Task<AuthResponse> Login(LoginRequest request)
    {
        await using var db = factory.Create(); await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT Id, Email, DisplayName, Role, PasswordSalt, PasswordHash FROM dbo.Users WHERE Email=@email AND IsActive=TRUE", db);
        cmd.Parameters.AddWithValue("@email", request.Email.Trim().ToLowerInvariant());
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync() || !Verify(request.Password, (byte[])r[4], (byte[])r[5])) throw new DomainException(401, "Невалиден имейл или парола.");
        return tokens.Create(r.GetInt32(0), r.GetString(1), r.GetString(3), r.GetString(2));
    }

    public async Task<AuthResponse> Register(RegisterRequest request, string role)
    {
        if (request.Email.Length > 180 || request.DisplayName.Trim().Length is < 2 or > 80 || request.Password.Length < 10)
            throw ServiceHelpers.BadRequest("Въведи валиден имейл, име (2–80 знака) и парола с поне 10 знака.");
        var salt = RandomNumberGenerator.GetBytes(16); var hash = Hash(request.Password, salt);
        await using var db = factory.Create(); await db.OpenAsync();
        try
        {
            await using var cmd = new NpgsqlCommand("INSERT INTO dbo.Users(Email,DisplayName,Role,PasswordSalt,PasswordHash) VALUES(@email,@name,@role,@salt,@hash) RETURNING Id", db);
            cmd.Parameters.AddWithValue("@email", request.Email.Trim().ToLowerInvariant()); cmd.Parameters.AddWithValue("@name", request.DisplayName.Trim());
            cmd.Parameters.AddWithValue("@role", role); cmd.Parameters.AddWithValue("@salt", salt); cmd.Parameters.AddWithValue("@hash", hash);
            var id = (int)(await cmd.ExecuteScalarAsync())!;
            return tokens.Create(id, request.Email.Trim().ToLowerInvariant(), role, request.DisplayName.Trim());
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation) { throw ServiceHelpers.Conflict("Този имейл вече е регистриран."); }
    }

    private static byte[] Hash(string password, byte[] salt) => Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 210_000, HashAlgorithmName.SHA256, 32);
    private static bool Verify(string password, byte[] salt, byte[] expected) => CryptographicOperations.FixedTimeEquals(Hash(password, salt), expected);
}

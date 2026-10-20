using System.Security.Claims;
using CollectionManager.Api.Data;
using Microsoft.Data.SqlClient;

namespace CollectionManager.Api.Services;

public sealed class ProfileService(SqlConnectionFactory factory)
{
    public Task<ProfileDto> Mine(ClaimsPrincipal user) => Get(user.UserId());

    public async Task<IReadOnlyList<object>> SearchPeople(string? query, ClaimsPrincipal user)
    {
        var term = query?.Trim() ?? "";
        if (term.Length < 2) return Array.Empty<object>();
        if (term.Length > 80) term = term[..80];
        await using var db = factory.Create(); await db.OpenAsync();
        const string sql = "SELECT TOP(20) Id,DisplayName,AvatarUrl,Location FROM dbo.Users WHERE IsActive=1 AND Id<>@me AND Role='Collector' AND (DisplayName LIKE @term OR Bio LIKE @term OR Location LIKE @term) ORDER BY CASE WHEN DisplayName LIKE @starts THEN 0 ELSE 1 END,DisplayName";
        await using var cmd = new SqlCommand(sql, db);
        cmd.Parameters.AddWithValue("@me", user.UserId()); cmd.Parameters.AddWithValue("@term", $"%{term}%"); cmd.Parameters.AddWithValue("@starts", $"{term}%");
        var results = new List<object>(); await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) results.Add(new { id=reader.GetInt32(0), displayName=reader.GetString(1), avatarUrl=reader.IsDBNull(2)?null:reader.GetString(2), location=reader.IsDBNull(3)?null:reader.GetString(3) });
        return results;
    }

    public async Task<ProfileDto> Get(int id)
    {
        await using var db = factory.Create(); await db.OpenAsync();
        const string sql = "SELECT Id,DisplayName,Role,Bio,AvatarUrl,Location,Website,CreatedAt FROM dbo.Users WHERE Id=@id AND IsActive=1";
        await using var cmd = new SqlCommand(sql, db); cmd.Parameters.AddWithValue("@id", id);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) throw ServiceHelpers.NotFound("Профилът не е намерен.");
        return new ProfileDto(r.GetInt32(0),r.GetString(1),r.GetString(2),r.IsDBNull(3)?null:r.GetString(3),r.IsDBNull(4)?null:r.GetString(4),r.IsDBNull(5)?null:r.GetString(5),r.IsDBNull(6)?null:r.GetString(6),r.GetDateTime(7));
    }

    public async Task<ProfileDto> Update(ClaimsPrincipal user, ProfileInput input)
    {
        var name = input.DisplayName?.Trim() ?? "";
        var bio = input.Bio?.Trim(); var location = input.Location?.Trim(); var website = input.Website?.Trim(); var avatar = input.AvatarUrl?.Trim();
        if (name.Length is < 2 or > 80 || (bio?.Length ?? 0) > 500 || (location?.Length ?? 0) > 80 || (website?.Length ?? 0) > 200 || (avatar?.Length ?? 0) > 500)
            throw ServiceHelpers.BadRequest("Провери името и дължините на полетата в профила.");
        if (!SafeImageUrl(avatar)) throw ServiceHelpers.BadRequest("Снимката трябва да е HTTPS адрес или качен файл.");
        if (!string.IsNullOrWhiteSpace(website) && (!Uri.TryCreate(website, UriKind.Absolute, out var webUri) || webUri.Scheme is not ("http" or "https")))
            throw ServiceHelpers.BadRequest("Уебсайтът трябва да започва с http:// или https://.");
        await using var db = factory.Create(); await db.OpenAsync();
        const string sql = "UPDATE dbo.Users SET DisplayName=@name,Bio=@bio,AvatarUrl=@avatar,Location=@location,Website=@website WHERE Id=@id AND IsActive=1";
        await using var cmd = new SqlCommand(sql, db);
        cmd.Parameters.AddWithValue("@name", name); cmd.Parameters.AddWithValue("@bio", (object?)NullIfBlank(bio) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@avatar", (object?)NullIfBlank(avatar) ?? DBNull.Value); cmd.Parameters.AddWithValue("@location", (object?)NullIfBlank(location) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@website", (object?)NullIfBlank(website) ?? DBNull.Value); cmd.Parameters.AddWithValue("@id", user.UserId());
        if (await cmd.ExecuteNonQueryAsync() == 0) throw ServiceHelpers.NotFound("Профилът не е намерен.");
        return await Get(user.UserId());
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private static bool SafeImageUrl(string? value) => string.IsNullOrWhiteSpace(value) || value.StartsWith("/uploads/", StringComparison.Ordinal) ||
        (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https");
}


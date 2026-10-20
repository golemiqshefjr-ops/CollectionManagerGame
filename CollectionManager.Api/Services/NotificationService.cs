using System.Security.Claims;
using CollectionManager.Api.Data;
using Microsoft.Data.SqlClient;

namespace CollectionManager.Api.Services;

public sealed class NotificationService(SqlConnectionFactory factory, ILogger<NotificationService> logger)
{
    public async Task<IReadOnlyList<NotificationDto>> List(ClaimsPrincipal user)
    {
        await using var db = factory.Create(); await db.OpenAsync();
        const string sql = "SELECT TOP(50) n.Id,n.Type,n.Title,n.Body,n.CreatedAt,n.IsRead,n.ItemId,n.ActorId,u.DisplayName FROM dbo.Notifications n LEFT JOIN dbo.Users u ON u.Id=n.ActorId WHERE n.RecipientId=@user ORDER BY n.CreatedAt DESC,n.Id DESC";
        await using var cmd = new SqlCommand(sql, db); cmd.Parameters.AddWithValue("@user", user.UserId());
        var rows = new List<NotificationDto>(); await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) rows.Add(new(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetDateTime(4), r.GetBoolean(5), r.IsDBNull(6) ? null : r.GetInt32(6), r.IsDBNull(7) ? null : r.GetInt32(7), r.IsDBNull(8) ? null : r.GetString(8)));
        return rows;
    }

    public async Task<object> UnreadCount(ClaimsPrincipal user)
    {
        await using var db = factory.Create(); await db.OpenAsync();
        await using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Notifications WHERE RecipientId=@user AND IsRead=0", db);
        cmd.Parameters.AddWithValue("@user", user.UserId());
        return new { count = Convert.ToInt32(await cmd.ExecuteScalarAsync()) };
    }

    public async Task MarkRead(int id, ClaimsPrincipal user)
    {
        await using var db = factory.Create(); await db.OpenAsync();
        await using var cmd = new SqlCommand("UPDATE dbo.Notifications SET IsRead=1 WHERE Id=@id AND RecipientId=@user", db);
        cmd.Parameters.AddWithValue("@id", id); cmd.Parameters.AddWithValue("@user", user.UserId()); await cmd.ExecuteNonQueryAsync();
    }

    public async Task MarkAllRead(ClaimsPrincipal user)
    {
        await using var db = factory.Create(); await db.OpenAsync();
        await using var cmd = new SqlCommand("UPDATE dbo.Notifications SET IsRead=1 WHERE RecipientId=@user AND IsRead=0", db);
        cmd.Parameters.AddWithValue("@user", user.UserId()); await cmd.ExecuteNonQueryAsync();
    }

    public async Task CreateAsync(int recipientId, int? actorId, int? itemId, string type, string title, string body)
    {
        if (actorId == recipientId) return;
        try
        {
            await using var db = factory.Create(); await db.OpenAsync();
            await using var cmd = new SqlCommand("INSERT dbo.Notifications(RecipientId,ActorId,ItemId,Type,Title,Body) VALUES(@recipient,@actor,@item,@type,@title,@body)", db);
            cmd.Parameters.AddWithValue("@recipient", recipientId); cmd.Parameters.AddWithValue("@actor", (object?)actorId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@item", (object?)itemId ?? DBNull.Value); cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@title", title); cmd.Parameters.AddWithValue("@body", body); await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex) { logger.LogWarning(ex, "Could not save notification for user {UserId}", recipientId); }
    }

    public async Task CreateForRoleAsync(string role, int? actorId, int? itemId, string type, string title, string body)
    {
        try
        {
            await using var db = factory.Create(); await db.OpenAsync();
            await using var cmd = new SqlCommand("INSERT dbo.Notifications(RecipientId,ActorId,ItemId,Type,Title,Body) SELECT Id,@actor,@item,@type,@title,@body FROM dbo.Users WHERE Role=@role AND IsActive=1 AND (@actor IS NULL OR Id<>@actor)", db);
            cmd.Parameters.AddWithValue("@role", role); cmd.Parameters.AddWithValue("@actor", (object?)actorId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@item", (object?)itemId ?? DBNull.Value); cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@title", title); cmd.Parameters.AddWithValue("@body", body); await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex) { logger.LogWarning(ex, "Could not save role notification for {Role}", role); }
    }
}


using CollectionManager.Api.Data;
using Npgsql;

namespace CollectionManager.Api.Services;

public sealed class AdminService(PostgresConnectionFactory factory)
{
    public async Task<IReadOnlyList<object>> Categories()
    {
        await using var db = factory.Create(); await db.OpenAsync(); await using var cmd = new NpgsqlCommand("SELECT Id,Name FROM dbo.Categories WHERE IsActive=TRUE ORDER BY Name", db);
        var result = new List<object>(); await using var r = await cmd.ExecuteReaderAsync(); while (await r.ReadAsync()) result.Add(new { id=r.GetInt32(0), name=r.GetString(1) }); return result;
    }
    public async Task<object> AddCategory(string name)
    {
        if (name.Trim().Length is < 2 or > 60) throw ServiceHelpers.BadRequest("Името трябва да е от 2 до 60 знака.");
        await using var db = factory.Create(); await db.OpenAsync(); await using var cmd = new NpgsqlCommand("INSERT INTO dbo.Categories(Name) VALUES(@name) RETURNING Id", db); cmd.Parameters.AddWithValue("@name",name.Trim());
        try { return new { id=await cmd.ExecuteScalarAsync(), name=name.Trim() }; } catch(PostgresException e) when(e.SqlState == PostgresErrorCodes.UniqueViolation) { throw ServiceHelpers.Conflict("Категорията вече съществува."); }
    }
    public async Task DeleteCategory(int id)
    {
        await using var db=factory.Create(); await db.OpenAsync(); await using var cmd=new NpgsqlCommand("UPDATE dbo.Categories SET IsActive=FALSE WHERE Id=@id AND NOT EXISTS(SELECT 1 FROM dbo.Items WHERE CategoryId=@id)",db); cmd.Parameters.AddWithValue("@id",id);
        if(await cmd.ExecuteNonQueryAsync()==0) throw ServiceHelpers.Conflict("Категорията липсва или се използва от предмет.");
    }
    public async Task<object> Summary()
    {
        await using var db=factory.Create(); await db.OpenAsync(); await using var cmd=new NpgsqlCommand("SELECT (SELECT COUNT(*)::int FROM dbo.Users),(SELECT COUNT(*)::int FROM dbo.Items),(SELECT COUNT(*)::int FROM dbo.Items WHERE Status='OnAuction'),(SELECT COUNT(*)::int FROM dbo.TradeOffers WHERE Status='Pending')",db);
        await using var r=await cmd.ExecuteReaderAsync(); await r.ReadAsync(); return new { users=r.GetInt32(0), items=r.GetInt32(1), auctions=r.GetInt32(2), pendingTrades=r.GetInt32(3) };
    }
}

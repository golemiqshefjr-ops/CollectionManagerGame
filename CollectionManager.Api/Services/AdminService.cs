using CollectionManager.Api.Data;
using Microsoft.Data.SqlClient;

namespace CollectionManager.Api.Services;

public sealed class AdminService(SqlConnectionFactory factory)
{
    public async Task<IReadOnlyList<object>> Categories()
    {
        await using var db = factory.Create(); await db.OpenAsync(); await using var cmd = new SqlCommand("SELECT Id,Name FROM dbo.Categories WHERE IsActive=1 ORDER BY Name", db);
        var result = new List<object>(); await using var r = await cmd.ExecuteReaderAsync(); while (await r.ReadAsync()) result.Add(new { id=r.GetInt32(0), name=r.GetString(1) }); return result;
    }
    public async Task<object> AddCategory(string name)
    {
        if (name.Trim().Length is < 2 or > 60) throw ServiceHelpers.BadRequest("Името трябва да е от 2 до 60 знака.");
        await using var db = factory.Create(); await db.OpenAsync(); await using var cmd = new SqlCommand("INSERT dbo.Categories(Name) OUTPUT INSERTED.Id VALUES(@name)", db); cmd.Parameters.AddWithValue("@name",name.Trim());
        try { return new { id=await cmd.ExecuteScalarAsync(), name=name.Trim() }; } catch(SqlException e) when(e.Number is 2601 or 2627) { throw ServiceHelpers.Conflict("Категорията вече съществува."); }
    }
    public async Task DeleteCategory(int id)
    {
        await using var db=factory.Create(); await db.OpenAsync(); await using var cmd=new SqlCommand("UPDATE dbo.Categories SET IsActive=0 WHERE Id=@id AND NOT EXISTS(SELECT 1 FROM dbo.Items WHERE CategoryId=@id)",db); cmd.Parameters.AddWithValue("@id",id);
        if(await cmd.ExecuteNonQueryAsync()==0) throw ServiceHelpers.Conflict("Категорията липсва или се използва от предмет.");
    }
    public async Task<object> Summary()
    {
        await using var db=factory.Create(); await db.OpenAsync(); await using var cmd=new SqlCommand("SELECT (SELECT COUNT(*) FROM dbo.Users),(SELECT COUNT(*) FROM dbo.Items),(SELECT COUNT(*) FROM dbo.Items WHERE Status='OnAuction'),(SELECT COUNT(*) FROM dbo.TradeOffers WHERE Status='Pending')",db);
        await using var r=await cmd.ExecuteReaderAsync(); await r.ReadAsync(); return new { users=r.GetInt32(0), items=r.GetInt32(1), auctions=r.GetInt32(2), pendingTrades=r.GetInt32(3) };
    }
}


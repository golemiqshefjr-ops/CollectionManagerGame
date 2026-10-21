using System.Security.Claims;
using CollectionManager.Api.Data;
using Npgsql;

namespace CollectionManager.Api.Services;

public sealed class CollectionService(PostgresConnectionFactory factory, NotificationService notifications)
{
    public async Task<IReadOnlyList<ItemDto>> List(ClaimsPrincipal user, string? status, int? categoryId, string? search)
    {
        var role = user.Role();
        await using var db = factory.Create(); await db.OpenAsync();
        var sql = "SELECT i.Id,i.Name,c.Name,i.Description,i.Condition,i.ImageUrl,i.Status,u.DisplayName,i.OwnerId,i.CreatedAt,i.AuctionEndsAt,(SELECT MAX(b.Amount) FROM dbo.Bids b WHERE b.ItemId=i.Id) CurrentBid,u.AvatarUrl FROM dbo.Items i JOIN dbo.Categories c ON c.Id=i.CategoryId JOIN dbo.Users u ON u.Id=i.OwnerId WHERE (@status IS NULL OR i.Status=@status) AND (@category IS NULL OR i.CategoryId=@category) AND (i.Name ILIKE @like OR c.Name ILIKE @like)";
        if (role == "Evaluator") sql += " AND i.Status='PendingApproval'";
        if (role == "Collector") sql += " AND (i.Status<>'PendingApproval' OR i.OwnerId=@viewer)";
        sql += " ORDER BY i.CreatedAt DESC";
        await using var cmd = new NpgsqlCommand(sql, db);
        cmd.Parameters.Add("@status", NpgsqlDbType.Text).Value = (object?)status ?? DBNull.Value;
        cmd.Parameters.Add("@category", NpgsqlDbType.Integer).Value = (object?)categoryId ?? DBNull.Value;
        cmd.Parameters.AddWithValue("@like", $"%{search}%");
        if (role == "Collector") cmd.Parameters.AddWithValue("@viewer", user.UserId());
        var rows = new List<ItemDto>(); await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) rows.Add(Read(r, null, null));
        return rows;
    }

    public async Task<ItemDto> Get(int id, ClaimsPrincipal user)
    {
        await using var db = factory.Create(); await db.OpenAsync();
        const string sql = "SELECT i.Id,i.Name,c.Name,i.Description,i.Condition,i.ImageUrl,i.Status,u.DisplayName,i.OwnerId,i.CreatedAt,i.AuctionEndsAt,(SELECT MAX(b.Amount) FROM dbo.Bids b WHERE b.ItemId=i.Id),u.AvatarUrl FROM dbo.Items i JOIN dbo.Categories c ON c.Id=i.CategoryId JOIN dbo.Users u ON u.Id=i.OwnerId WHERE i.Id=@id";
        await using var cmd = new NpgsqlCommand(sql, db); cmd.Parameters.AddWithValue("@id", id);
        await using var r = await cmd.ExecuteReaderAsync(); if (!await r.ReadAsync()) throw ServiceHelpers.NotFound();
        var item = Read(r, null, null); var viewer = user.UserId(); bool show = false;
        if (item.Status == "Sold" && item.OwnerId == viewer) show = true;
        if (!show && item.Status == "Traded")
        {
            await r.CloseAsync();
            await using var c = new NpgsqlCommand("SELECT COUNT(*) FROM dbo.TradeOffers WHERE Status='Accepted' AND (WantedItemId=@id OR OfferedItemId=@id) AND (SenderId=@u OR RecipientId=@u)", db);
            c.Parameters.AddWithValue("@id", id); c.Parameters.AddWithValue("@u", viewer); show = Convert.ToInt32(await c.ExecuteScalarAsync()) > 0;
        }
        if (!show) return item;
        await using var contact = new NpgsqlCommand("SELECT Phone,DeliveryAddress FROM dbo.Items WHERE Id=@id", db); contact.Parameters.AddWithValue("@id", id);
        await using var cr = await contact.ExecuteReaderAsync(); if (!await cr.ReadAsync()) return item;
        return item with { ContactPhone = cr.IsDBNull(0) ? null : cr.GetString(0), DeliveryAddress = cr.IsDBNull(1) ? null : cr.GetString(1) };
    }

    public async Task<ItemDto> Create(ItemInput x, ClaimsPrincipal user)
    {
        Validate(x); await using var db = factory.Create(); await db.OpenAsync();
        const string sql = "INSERT INTO dbo.Items(Name,CategoryId,OwnerId,Description,Condition,ImageUrl,DeliveryAddress,Phone,Status) SELECT @n,@c,@u,@d,@cond,@img,@addr,@phone,'InCollection' WHERE EXISTS(SELECT 1 FROM dbo.Categories WHERE Id=@c AND IsActive=TRUE) RETURNING Id";
        await using var cmd = new NpgsqlCommand(sql, db); AddItemParams(cmd, x, user.UserId()); var inserted = await cmd.ExecuteScalarAsync();
        if(inserted is null or DBNull) throw ServiceHelpers.BadRequest("Избраната категория липсва или е неактивна."); var id = Convert.ToInt32(inserted);
        return await Get(id, user);
    }

    public async Task<ItemDto> Update(int id, ItemInput x, ClaimsPrincipal user)
    {
        Validate(x); await using var db = factory.Create(); await db.OpenAsync();
        await EnsureOwnerStatus(db,id,user.UserId(),"InCollection");
        await using var cmd = new NpgsqlCommand("UPDATE dbo.Items SET Name=@n,CategoryId=@c,Description=@d,Condition=@cond,ImageUrl=@img,DeliveryAddress=COALESCE(@addr,DeliveryAddress),Phone=COALESCE(@phone,Phone) WHERE Id=@id AND OwnerId=@u AND Status='InCollection' AND EXISTS(SELECT 1 FROM dbo.Categories WHERE Id=@c AND IsActive=TRUE)", db);
        AddItemParams(cmd, x, user.UserId()); cmd.Parameters.AddWithValue("@id", id); if (await cmd.ExecuteNonQueryAsync() == 0) throw ServiceHelpers.Conflict("Предметът или категорията са променени. Зареди страницата отново.");
        return await Get(id, user);
    }

    public async Task Delete(int id, ClaimsPrincipal user)
    {
        await using var db = factory.Create(); await db.OpenAsync();
        await EnsureOwnerStatus(db,id,user.UserId(),"InCollection");
        await using var cmd = new NpgsqlCommand("DELETE FROM dbo.Items WHERE Id=@id AND OwnerId=@u AND Status='InCollection'", db);
        cmd.Parameters.AddWithValue("@id", id); cmd.Parameters.AddWithValue("@u", user.UserId()); if (await cmd.ExecuteNonQueryAsync() == 0) throw ServiceHelpers.Conflict("Предметът вече е променен. Зареди страницата отново.");
    }

    public Task<ItemDto> ListForTrade(int id, ClaimsPrincipal user) => ChangeOwnedStatus(id, "InCollection", "ForTrade", user);
    public async Task<ItemDto> RequestAuction(int id, ClaimsPrincipal user)
    {
        var item = await ChangeOwnedStatus(id, "InCollection", "PendingApproval", user);
        await notifications.CreateForRoleAsync("Evaluator", user.UserId(), id, "auction-review", "Нова заявка за търг", "Предмет очаква оценка.");
        return item;
    }
    public Task<ItemDto> WithdrawTradeListing(int id, ClaimsPrincipal user) => ChangeOwnedStatus(id, "ForTrade", "InCollection", user);
    public Task<ItemDto> WithdrawAuctionRequest(int id, ClaimsPrincipal user) => ChangeOwnedStatus(id, "PendingApproval", "InCollection", user);

    public async Task<ItemDto> ReviewAuction(int id, bool approve, ClaimsPrincipal user)
    {
        await using var db = factory.Create(); await db.OpenAsync();
        var status = approve ? "OnAuction" : "InCollection";
        await using var cmd = new NpgsqlCommand("UPDATE dbo.Items SET Status=@s, AuctionEndsAt=CASE WHEN @s='OnAuction' THEN CURRENT_TIMESTAMP + INTERVAL '7 days' ELSE NULL END WHERE Id=@id AND Status='PendingApproval'", db);
        cmd.Parameters.AddWithValue("@s", status); cmd.Parameters.AddWithValue("@id", id);
        if (await cmd.ExecuteNonQueryAsync() == 0) throw ServiceHelpers.Conflict("За одобрение се приемат само предмети PendingApproval.");
        var item = await Get(id, user);
        await notifications.CreateAsync(item.OwnerId, user.UserId(), id, approve ? "auction-approved" : "auction-rejected",
            approve ? "Заявката е одобрена" : "Предметът е върнат в колекцията",
            approve ? "Търгът ти е одобрен и вече е активен." : "Оценителят върна предмета в колекцията ти.");
        return item;
    }

    private async Task<ItemDto> ChangeOwnedStatus(int id, string from, string to, ClaimsPrincipal user)
    {
        await using var db = factory.Create(); await db.OpenAsync();
        await EnsureOwnerStatus(db,id,user.UserId(),from);
        await using var cmd = new NpgsqlCommand("UPDATE dbo.Items SET Status=@to, AuctionEndsAt=CASE WHEN @to='PendingApproval' THEN NULL ELSE AuctionEndsAt END WHERE Id=@id AND OwnerId=@u AND Status=@from", db);
        cmd.Parameters.AddWithValue("@id", id); cmd.Parameters.AddWithValue("@u", user.UserId()); cmd.Parameters.AddWithValue("@from", from); cmd.Parameters.AddWithValue("@to", to);
        if (await cmd.ExecuteNonQueryAsync() == 0) throw ServiceHelpers.Conflict("Промяната е възможна само за твой предмет със статус InCollection.");
        return await Get(id, user);
    }

    private static async Task EnsureOwnerStatus(NpgsqlConnection db,int id,int ownerId,string expectedStatus)
    {
        await using var cmd=new NpgsqlCommand("SELECT OwnerId,Status FROM dbo.Items WHERE Id=@id",db); cmd.Parameters.AddWithValue("@id",id); await using var r=await cmd.ExecuteReaderAsync();
        if(!await r.ReadAsync()) throw ServiceHelpers.NotFound("Предметът не е намерен.");
        if(r.GetInt32(0)!=ownerId) throw ServiceHelpers.Forbidden("Можеш да управляваш само собствените си предмети.");
        if(r.GetString(1)!=expectedStatus) throw ServiceHelpers.Conflict($"Операцията изисква статус {expectedStatus}.");
    }

    private static ItemDto Read(NpgsqlDataReader r, string? phone, string? address)
    {
        var status = r.GetString(6);
        var highestBidInLeva = r.IsDBNull(11) ? (decimal?)null : r.GetDecimal(11);
        decimal? currentBidInEuro = highestBidInLeva is null ? null : AuctionCurrency.ToEuro(highestBidInLeva.Value);
        decimal? minimumNextBidInEuro = status == "OnAuction" ? AuctionCurrency.MinimumBidInEuroAfter(highestBidInLeva) : null;
        return new(r.GetInt32(0),r.GetString(1),r.GetString(2),r.IsDBNull(3)?null:r.GetString(3),r.IsDBNull(4)?null:r.GetString(4),r.IsDBNull(5)?null:r.GetString(5),status,r.GetString(7),r.GetInt32(8),r.GetDateTime(9),r.IsDBNull(10)?null:r.GetDateTime(10),currentBidInEuro,minimumNextBidInEuro,phone,address,r.IsDBNull(12)?null:r.GetString(12));
    }
    private static void Validate(ItemInput x) { if (x.Name.Trim().Length is < 2 or > 120 || x.CategoryId < 1) throw ServiceHelpers.BadRequest("Името или категорията са невалидни."); }
    private static void AddItemParams(NpgsqlCommand c, ItemInput x, int uid) { c.Parameters.AddWithValue("@n",x.Name.Trim()); c.Parameters.AddWithValue("@c",x.CategoryId); c.Parameters.AddWithValue("@u",uid); c.Parameters.AddWithValue("@d",(object?)x.Description??DBNull.Value); c.Parameters.AddWithValue("@cond",(object?)x.Condition??DBNull.Value); c.Parameters.AddWithValue("@img",(object?)x.ImageUrl??DBNull.Value); c.Parameters.AddWithValue("@addr",(object?)x.DeliveryAddress??DBNull.Value); c.Parameters.AddWithValue("@phone",(object?)x.Phone??DBNull.Value); }
}

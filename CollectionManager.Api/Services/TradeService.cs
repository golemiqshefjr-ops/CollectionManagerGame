using System.Data;
using System.Security.Claims;
using CollectionManager.Api.Data;
using Microsoft.Data.SqlClient;

namespace CollectionManager.Api.Services;

public sealed class TradeService(SqlConnectionFactory factory, NotificationService notifications)
{
    public async Task<object> MyTrades(ClaimsPrincipal user)
    {
        await using var db=factory.Create(); await db.OpenAsync();
        await using var cmd=new SqlCommand("SELECT t.Id,t.WantedItemId,w.Name,t.OfferedItemId,o.Name,t.SenderId,t.RecipientId,t.Status,t.Note,t.CreatedAt FROM dbo.TradeOffers t JOIN dbo.Items w ON w.Id=t.WantedItemId JOIN dbo.Items o ON o.Id=t.OfferedItemId WHERE t.SenderId=@u OR t.RecipientId=@u ORDER BY t.CreatedAt DESC",db);
        cmd.Parameters.AddWithValue("@u",user.UserId()); var rows=new List<object>(); await using var r=await cmd.ExecuteReaderAsync();
        while(await r.ReadAsync()) rows.Add(new { id=r.GetInt32(0), wantedItemId=r.GetInt32(1), wantedItem=r.GetString(2), offeredItemId=r.GetInt32(3), offeredItem=r.GetString(4), senderId=r.GetInt32(5), recipientId=r.GetInt32(6), status=r.GetString(7), note=r.IsDBNull(8)?null:r.GetString(8), createdAt=r.GetDateTime(9) });
        return rows;
    }

    public async Task<object> ItemActivity(int id, ClaimsPrincipal user)
    {
        var uid=user.UserId(); await using var db=factory.Create(); await db.OpenAsync();
        await using var check=new SqlCommand("SELECT OwnerId,Status,(SELECT MAX(Amount) FROM dbo.Bids WHERE ItemId=@id) FROM dbo.Items WHERE Id=@id",db); check.Parameters.AddWithValue("@id",id);
        await using var cr=await check.ExecuteReaderAsync(); if(!await cr.ReadAsync()) throw ServiceHelpers.NotFound(); var owner=cr.GetInt32(0); var status=cr.GetString(1); var highest=cr.IsDBNull(2)?(decimal?)null:cr.GetDecimal(2); await cr.CloseAsync();
        if(owner!=uid && status is not ("ForTrade" or "OnAuction" or "Sold") && !await IsParticipant(db,id,uid)) throw ServiceHelpers.Forbidden("Тази активност е видима само за участниците.");
        var offers=new List<object>();
        await using(var cmd=new SqlCommand("SELECT t.Id,t.WantedItemId,w.Name,t.OfferedItemId,o.Name,t.SenderId,u.DisplayName,t.Status,t.Note,t.CreatedAt FROM dbo.TradeOffers t JOIN dbo.Items w ON w.Id=t.WantedItemId JOIN dbo.Items o ON o.Id=t.OfferedItemId JOIN dbo.Users u ON u.Id=t.SenderId WHERE (t.WantedItemId=@id OR t.OfferedItemId=@id) AND (t.RecipientId=@u OR t.SenderId=@u) ORDER BY t.CreatedAt DESC",db))
        { cmd.Parameters.AddWithValue("@id",id); cmd.Parameters.AddWithValue("@u",uid); await using var r=await cmd.ExecuteReaderAsync(); while(await r.ReadAsync()) offers.Add(new { id=r.GetInt32(0),wantedItemId=r.GetInt32(1),wantedItem=r.GetString(2),offeredItemId=r.GetInt32(3),offeredItem=r.GetString(4),senderId=r.GetInt32(5),senderName=r.GetString(6),status=r.GetString(7),note=r.IsDBNull(8)?null:r.GetString(8),createdAt=r.GetDateTime(9) }); }
        var bids=new List<object>();
        if(status=="OnAuction" || status=="Sold")
        { await using var cmd=new SqlCommand("SELECT TOP(10) b.Amount,b.CreatedAt,u.DisplayName FROM dbo.Bids b JOIN dbo.Users u ON u.Id=b.BidderId WHERE b.ItemId=@id ORDER BY b.Amount DESC,b.CreatedAt",db); cmd.Parameters.AddWithValue("@id",id); await using var r=await cmd.ExecuteReaderAsync(); while(await r.ReadAsync()) bids.Add(new {amount=AuctionCurrency.ToEuro(r.GetDecimal(0)),createdAt=r.GetDateTime(1),bidder=r.GetString(2)}); }
        decimal? highestBidInEuro = highest is null ? null : AuctionCurrency.ToEuro(highest.Value);
        return new { offers, bids, highestBid=highestBidInEuro };
    }

    private static async Task<bool> IsParticipant(SqlConnection db,int item,int user)
    { await using var cmd=new SqlCommand("SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.TradeOffers WHERE (WantedItemId=@i OR OfferedItemId=@i) AND Status='Accepted' AND (SenderId=@u OR RecipientId=@u)) THEN 1 ELSE 0 END",db); cmd.Parameters.AddWithValue("@i",item); cmd.Parameters.AddWithValue("@u",user); return Convert.ToInt32(await cmd.ExecuteScalarAsync()) == 1; }

    public async Task<TradeDto> CreateTrade(TradeInput input, ClaimsPrincipal user)
    {
        var sender = user.UserId();
        if (input.WantedItemId == input.OfferedItemId) throw ServiceHelpers.BadRequest("Избери два различни предмета.");
        await using var db = factory.Create(); await db.OpenAsync();
        await using var cmd = new SqlCommand("INSERT dbo.TradeOffers(WantedItemId,OfferedItemId,SenderId,RecipientId,Note,Status) SELECT w.Id,o.Id,@sender,w.OwnerId,@note,'Pending' FROM dbo.Items w CROSS JOIN dbo.Items o WHERE w.Id=@wanted AND o.Id=@offered AND w.OwnerId<>@sender AND o.OwnerId=@sender AND w.Status='ForTrade' AND o.Status='InCollection'; SELECT CAST(SCOPE_IDENTITY() AS int)", db);
        cmd.Parameters.AddWithValue("@sender", sender); cmd.Parameters.AddWithValue("@wanted", input.WantedItemId); cmd.Parameters.AddWithValue("@offered", input.OfferedItemId); cmd.Parameters.AddWithValue("@note", (object?)input.Note ?? DBNull.Value);
        var result = await cmd.ExecuteScalarAsync(); if (result is null or DBNull) throw ServiceHelpers.Conflict("Размяна може да се предложи само за чужд предмет ForTrade срещу твой предмет InCollection.");
        var trade = await ReadTrade((int)result, db);
        await using var owner = new SqlCommand("SELECT OwnerId FROM dbo.Items WHERE Id=@id", db); owner.Parameters.AddWithValue("@id", input.WantedItemId);
        var recipientId = Convert.ToInt32(await owner.ExecuteScalarAsync());
        await notifications.CreateAsync(recipientId, sender, input.WantedItemId, "trade-offer", "Ново предложение за размяна", "Колекционер ти изпрати предложение.");
        return trade;
    }

    public async Task<TradeDto> AcceptTrade(int id, ClaimsPrincipal user)
    {
        await using var db = factory.Create(); await db.OpenAsync();
        await using var cmd = new SqlCommand("dbo.AcceptTradeOffer", db) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@TradeOfferId", id); cmd.Parameters.AddWithValue("@OwnerUserId", user.UserId());
        try { await cmd.ExecuteNonQueryAsync(); }
        catch (SqlException e) when (e.Number is 51000 or 51001)
        {
            if (e.Number == 51001) throw ServiceHelpers.Forbidden(e.Message);
            throw ServiceHelpers.Conflict(e.Message);
        }
        var trade = await ReadTrade(id, db);
        await notifications.CreateAsync(trade.SenderId, user.UserId(), trade.WantedItemId, "trade-accepted", "Предложението е прието", "Размяната ти беше приета.");
        return trade;
    }

    public async Task<int> CloseDueAuctions()
    {
        await using var db = factory.Create(); await db.OpenAsync();
        var categories=new DataTable(); categories.Columns.Add("Name",typeof(string));
        await using(var readCategories=new SqlCommand("SELECT Name FROM dbo.Categories WHERE IsActive=1",db))
        await using(var reader=await readCategories.ExecuteReaderAsync()) while(await reader.ReadAsync()) categories.Rows.Add(reader.GetString(0));
        await using var cmd = new SqlCommand("dbo.CloseAuction", db) { CommandType = CommandType.StoredProcedure };
        var categoryParameter=cmd.Parameters.AddWithValue("@Categories",categories); categoryParameter.SqlDbType=SqlDbType.Structured; categoryParameter.TypeName="dbo.CategoryNameList";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<object> PlaceBid(int itemId, decimal amount, ClaimsPrincipal user)
    {
        if (amount <= 0 || decimal.Round(amount, 2) != amount) throw ServiceHelpers.BadRequest("Въведи положителна сума в евро със стотинки.");
        var amountInLeva = AuctionCurrency.ToLeva(amount);
        var bidder = user.UserId(); await using var db = factory.Create(); await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            await using var current = new SqlCommand("SELECT OwnerId,AuctionEndsAt,Status,(SELECT MAX(Amount) FROM dbo.Bids WITH (UPDLOCK,HOLDLOCK) WHERE ItemId=i.Id),(SELECT TOP(1) BidderId FROM dbo.Bids WITH (UPDLOCK,HOLDLOCK) WHERE ItemId=i.Id ORDER BY Amount DESC,CreatedAt) FROM dbo.Items i WITH (UPDLOCK,HOLDLOCK) WHERE Id=@id", db, (SqlTransaction)tx);
            current.Parameters.AddWithValue("@id", itemId); await using var r = await current.ExecuteReaderAsync();
            if (!await r.ReadAsync()) throw ServiceHelpers.NotFound("Предметът не е намерен.");
            var owner = r.GetInt32(0); var end = r.IsDBNull(1) ? DateTime.MinValue : r.GetDateTime(1); var status = r.GetString(2); var highest = r.IsDBNull(3) ? 0m : r.GetDecimal(3); var previousBidder = r.IsDBNull(4) ? (int?)null : r.GetInt32(4); await r.CloseAsync();
            if (status != "OnAuction" || end <= DateTime.UtcNow) throw ServiceHelpers.Conflict("Наддава се само по активен търг преди крайния час.");
            if (owner == bidder) throw ServiceHelpers.Forbidden("Не можеш да наддаваш за собствен предмет.");
            // The database retains legacy BGN values; bidding is entered and reported in EUR.
            var minimum = AuctionCurrency.MinimumBidInLevaAfter(highest == 0 ? null : highest);
            if (amountInLeva < minimum) throw ServiceHelpers.Conflict($"Новото наддаване трябва да е поне {AuctionCurrency.MinimumBidInEuroAfter(highest == 0 ? null : highest):0.00} €.");
            await using var add = new SqlCommand("INSERT dbo.Bids(ItemId,BidderId,Amount) VALUES(@item,@user,@amount)", db, (SqlTransaction)tx);
            add.Parameters.AddWithValue("@item", itemId); add.Parameters.AddWithValue("@user", bidder); add.Parameters.AddWithValue("@amount", amountInLeva); await add.ExecuteNonQueryAsync();
            await tx.CommitAsync();
            await notifications.CreateAsync(owner, bidder, itemId, "auction-bid", "Ново наддаване", "Има нова оферта за твоя предмет.");
            if (previousBidder is int previous && previous != bidder)
                await notifications.CreateAsync(previous, bidder, itemId, "auction-outbid", "Наддаването ти е надминато", "Вече има по-висока оферта за този предмет.");
            return new { itemId, amount = AuctionCurrency.ToEuro(amountInLeva), minimumNextBid = AuctionCurrency.MinimumBidInEuroAfter(amountInLeva) };
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    private static async Task<TradeDto> ReadTrade(int id, SqlConnection db)
    {
        await using var cmd = new SqlCommand("SELECT Id,WantedItemId,OfferedItemId,SenderId,Status,Note,CreatedAt FROM dbo.TradeOffers WHERE Id=@id", db); cmd.Parameters.AddWithValue("@id", id);
        await using var r = await cmd.ExecuteReaderAsync(); if (!await r.ReadAsync()) throw ServiceHelpers.NotFound();
        return new TradeDto(r.GetInt32(0),r.GetInt32(1),r.GetInt32(2),r.GetInt32(3),r.GetString(4),r.IsDBNull(5)?null:r.GetString(5),r.GetDateTime(6));
    }
}


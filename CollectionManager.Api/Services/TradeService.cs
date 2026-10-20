using System.Data;
using System.Security.Claims;
using CollectionManager.Api.Data;
using Npgsql;

namespace CollectionManager.Api.Services;

public sealed class TradeService(PostgresConnectionFactory factory, NotificationService notifications)
{
    public async Task<object> MyTrades(ClaimsPrincipal user)
    {
        await using var db = factory.Create(); await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT t.Id,t.WantedItemId,w.Name,t.OfferedItemId,o.Name,t.SenderId,t.RecipientId,t.Status,t.Note,t.CreatedAt FROM dbo.TradeOffers t JOIN dbo.Items w ON w.Id=t.WantedItemId JOIN dbo.Items o ON o.Id=t.OfferedItemId WHERE t.SenderId=@u OR t.RecipientId=@u ORDER BY t.CreatedAt DESC", db);
        cmd.Parameters.AddWithValue("@u", user.UserId()); var rows = new List<object>(); await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) rows.Add(new { id=r.GetInt32(0), wantedItemId=r.GetInt32(1), wantedItem=r.GetString(2), offeredItemId=r.GetInt32(3), offeredItem=r.GetString(4), senderId=r.GetInt32(5), recipientId=r.GetInt32(6), status=r.GetString(7), note=r.IsDBNull(8)?null:r.GetString(8), createdAt=r.GetDateTime(9) });
        return rows;
    }

    public async Task<object> ItemActivity(int id, ClaimsPrincipal user)
    {
        var uid = user.UserId(); await using var db = factory.Create(); await db.OpenAsync();
        await using var check = new NpgsqlCommand("SELECT OwnerId,Status,(SELECT MAX(Amount) FROM dbo.Bids WHERE ItemId=@id) FROM dbo.Items WHERE Id=@id", db); check.Parameters.AddWithValue("@id", id);
        await using var cr = await check.ExecuteReaderAsync(); if (!await cr.ReadAsync()) throw ServiceHelpers.NotFound(); var owner = cr.GetInt32(0); var status = cr.GetString(1); var highest = cr.IsDBNull(2) ? (decimal?)null : cr.GetDecimal(2); await cr.CloseAsync();
        if (owner != uid && status is not ("ForTrade" or "OnAuction" or "Sold") && !await IsParticipant(db, id, uid)) throw ServiceHelpers.Forbidden("Тази активност е видима само за участниците.");
        var offers = new List<object>();
        await using (var cmd = new NpgsqlCommand("SELECT t.Id,t.WantedItemId,w.Name,t.OfferedItemId,o.Name,t.SenderId,u.DisplayName,t.Status,t.Note,t.CreatedAt FROM dbo.TradeOffers t JOIN dbo.Items w ON w.Id=t.WantedItemId JOIN dbo.Items o ON o.Id=t.OfferedItemId JOIN dbo.Users u ON u.Id=t.SenderId WHERE (t.WantedItemId=@id OR t.OfferedItemId=@id) AND (t.RecipientId=@u OR t.SenderId=@u) ORDER BY t.CreatedAt DESC", db))
        {
            cmd.Parameters.AddWithValue("@id", id); cmd.Parameters.AddWithValue("@u", uid); await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) offers.Add(new { id=r.GetInt32(0), wantedItemId=r.GetInt32(1), wantedItem=r.GetString(2), offeredItemId=r.GetInt32(3), offeredItem=r.GetString(4), senderId=r.GetInt32(5), senderName=r.GetString(6), status=r.GetString(7), note=r.IsDBNull(8)?null:r.GetString(8), createdAt=r.GetDateTime(9) });
        }
        var bids = new List<object>();
        if (status is "OnAuction" or "Sold")
        {
            await using var cmd = new NpgsqlCommand("SELECT b.Amount,b.CreatedAt,u.DisplayName FROM dbo.Bids b JOIN dbo.Users u ON u.Id=b.BidderId WHERE b.ItemId=@id ORDER BY b.Amount DESC,b.CreatedAt LIMIT 10", db);
            cmd.Parameters.AddWithValue("@id", id); await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) bids.Add(new { amount=AuctionCurrency.ToEuro(r.GetDecimal(0)), createdAt=r.GetDateTime(1), bidder=r.GetString(2) });
        }
        decimal? highestBidInEuro = highest is null ? null : AuctionCurrency.ToEuro(highest.Value);
        return new { offers, bids, highestBid=highestBidInEuro };
    }

    private static async Task<bool> IsParticipant(NpgsqlConnection db, int item, int user)
    {
        await using var cmd = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM dbo.TradeOffers WHERE (WantedItemId=@i OR OfferedItemId=@i) AND Status='Accepted' AND (SenderId=@u OR RecipientId=@u))", db);
        cmd.Parameters.AddWithValue("@i", item); cmd.Parameters.AddWithValue("@u", user); return (bool)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<TradeDto> CreateTrade(TradeInput input, ClaimsPrincipal user)
    {
        var sender = user.UserId();
        if (input.WantedItemId == input.OfferedItemId) throw ServiceHelpers.BadRequest("Избери два различни предмета.");
        await using var db = factory.Create(); await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("INSERT INTO dbo.TradeOffers(WantedItemId,OfferedItemId,SenderId,RecipientId,Note,Status) SELECT w.Id,o.Id,@sender,w.OwnerId,@note,'Pending' FROM dbo.Items w CROSS JOIN dbo.Items o WHERE w.Id=@wanted AND o.Id=@offered AND w.OwnerId<>@sender AND o.OwnerId=@sender AND w.Status='ForTrade' AND o.Status='InCollection' RETURNING Id", db);
        cmd.Parameters.AddWithValue("@sender", sender); cmd.Parameters.AddWithValue("@wanted", input.WantedItemId); cmd.Parameters.AddWithValue("@offered", input.OfferedItemId); cmd.Parameters.AddWithValue("@note", (object?)input.Note ?? DBNull.Value);
        var result = await cmd.ExecuteScalarAsync(); if (result is null or DBNull) throw ServiceHelpers.Conflict("Размяна може да се предложи само за чужд предмет ForTrade срещу твой предмет InCollection.");
        var trade = await ReadTrade(Convert.ToInt32(result), db);
        await using (var owner = new NpgsqlCommand("SELECT OwnerId FROM dbo.Items WHERE Id=@id", db))
        {
            owner.Parameters.AddWithValue("@id", input.WantedItemId);
            var recipient = Convert.ToInt32(await owner.ExecuteScalarAsync());
            await notifications.CreateAsync(recipient, sender, input.WantedItemId, "trade-offer", "Ново предложение за размяна", "Колекционер ти изпрати предложение.");
        }
        return trade;
    }

    public async Task<TradeDto> AcceptTrade(int id, ClaimsPrincipal user)
    {
        var currentUser = user.UserId(); await using var db = factory.Create(); await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        TradeDto trade;
        try
        {
            int wanted; int offered; int sender; int recipient;
            await using (var offer = new NpgsqlCommand("SELECT WantedItemId,OfferedItemId,SenderId,RecipientId FROM dbo.TradeOffers WHERE Id=@id AND Status='Pending' FOR UPDATE", db, tx))
            {
                offer.Parameters.AddWithValue("@id", id); await using var r = await offer.ExecuteReaderAsync();
                if (!await r.ReadAsync()) throw ServiceHelpers.Conflict("Предложението липсва или вече не е чакащо.");
                wanted = r.GetInt32(0); offered = r.GetInt32(1); sender = r.GetInt32(2); recipient = r.GetInt32(3);
            }
            if (recipient != currentUser) throw ServiceHelpers.Forbidden("Само собственикът на желания предмет може да приеме размяната.");

            var items = new Dictionary<int, (int OwnerId, string Status)>();
            await using (var lockItems = new NpgsqlCommand("SELECT Id,OwnerId,Status FROM dbo.Items WHERE Id=ANY(@ids) ORDER BY Id FOR UPDATE", db, tx))
            {
                lockItems.Parameters.AddWithValue("@ids", new[] { wanted, offered }); await using var r = await lockItems.ExecuteReaderAsync();
                while (await r.ReadAsync()) items[r.GetInt32(0)] = (r.GetInt32(1), r.GetString(2));
            }
            if (!items.TryGetValue(wanted, out var wantedItem) || wantedItem.OwnerId != recipient || wantedItem.Status != "ForTrade" ||
                !items.TryGetValue(offered, out var offeredItem) || offeredItem.OwnerId != sender || offeredItem.Status != "InCollection")
                throw ServiceHelpers.Conflict("Предметите вече не отговарят на условията за размяна.");

            await using (var updateWanted = new NpgsqlCommand("UPDATE dbo.Items SET OwnerId=@owner,Status='Traded' WHERE Id=@id", db, tx))
            {
                updateWanted.Parameters.AddWithValue("@owner", sender); updateWanted.Parameters.AddWithValue("@id", wanted); await updateWanted.ExecuteNonQueryAsync();
            }
            await using (var updateOffered = new NpgsqlCommand("UPDATE dbo.Items SET OwnerId=@owner,Status='Traded' WHERE Id=@id", db, tx))
            {
                updateOffered.Parameters.AddWithValue("@owner", recipient); updateOffered.Parameters.AddWithValue("@id", offered); await updateOffered.ExecuteNonQueryAsync();
            }
            await using (var accept = new NpgsqlCommand("UPDATE dbo.TradeOffers SET Status='Accepted' WHERE Id=@id", db, tx))
            {
                accept.Parameters.AddWithValue("@id", id); await accept.ExecuteNonQueryAsync();
            }
            await using (var decline = new NpgsqlCommand("UPDATE dbo.TradeOffers SET Status='Declined' WHERE Id<>@id AND Status='Pending' AND (WantedItemId=ANY(@items) OR OfferedItemId=ANY(@items))", db, tx))
            {
                decline.Parameters.AddWithValue("@id", id); decline.Parameters.AddWithValue("@items", new[] { wanted, offered }); await decline.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
            trade = await ReadTrade(id, db);
        }
        catch { await tx.RollbackAsync(); throw; }

        await notifications.CreateAsync(trade.SenderId, currentUser, trade.WantedItemId, "trade-accepted", "Предложението е прието", "Размяната ти беше приета.");
        return trade;
    }

    public async Task<int> CloseDueAuctions()
    {
        await using var db = factory.Create(); await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var due = new List<(int ItemId, int? WinnerId)>();
            const string selectSql = "SELECT i.Id,(SELECT b.BidderId FROM dbo.Bids b WHERE b.ItemId=i.Id ORDER BY b.Amount DESC,b.CreatedAt,b.Id LIMIT 1) FROM dbo.Items i JOIN dbo.Categories c ON c.Id=i.CategoryId WHERE i.Status='OnAuction' AND i.AuctionEndsAt<=CURRENT_TIMESTAMP AND c.IsActive=TRUE ORDER BY i.Id FOR UPDATE OF i SKIP LOCKED";
            await using (var select = new NpgsqlCommand(selectSql, db, tx))
            await using (var r = await select.ExecuteReaderAsync())
                while (await r.ReadAsync()) due.Add((r.GetInt32(0), r.IsDBNull(1) ? null : r.GetInt32(1)));

            foreach (var auction in due)
            {
                const string updateSql = "UPDATE dbo.Items SET OwnerId=COALESCE(@winner,OwnerId),Status=CASE WHEN @winner IS NULL THEN 'InCollection' ELSE 'Sold' END,AuctionEndsAt=NULL WHERE Id=@id AND Status='OnAuction'";
                await using var update = new NpgsqlCommand(updateSql, db, tx);
                update.Parameters.AddWithValue("@winner", (object?)auction.WinnerId ?? DBNull.Value);
                update.Parameters.AddWithValue("@id", auction.ItemId); await update.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync(); return due.Count;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<object> PlaceBid(int itemId, decimal amount, ClaimsPrincipal user)
    {
        if (amount <= 0 || decimal.Round(amount, 2) != amount) throw ServiceHelpers.BadRequest("Въведи положителна сума в евро със стотинки.");
        var amountInLeva = AuctionCurrency.ToLeva(amount);
        var bidder = user.UserId(); await using var db = factory.Create(); await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        int owner; DateTime end; string status; decimal highest; int? previousBidder;
        try
        {
            await using (var item = new NpgsqlCommand("SELECT OwnerId,AuctionEndsAt,Status FROM dbo.Items WHERE Id=@id FOR UPDATE", db, tx))
            {
                item.Parameters.AddWithValue("@id", itemId); await using var r = await item.ExecuteReaderAsync();
                if (!await r.ReadAsync()) throw ServiceHelpers.NotFound("Предметът не е намерен.");
                owner = r.GetInt32(0); end = r.IsDBNull(1) ? DateTime.MinValue : r.GetDateTime(1); status = r.GetString(2);
            }
            await using (var currentBid = new NpgsqlCommand("SELECT (SELECT MAX(Amount) FROM dbo.Bids WHERE ItemId=@id),(SELECT BidderId FROM dbo.Bids WHERE ItemId=@id ORDER BY Amount DESC,CreatedAt LIMIT 1)", db, tx))
            {
                currentBid.Parameters.AddWithValue("@id", itemId); await using var r = await currentBid.ExecuteReaderAsync(); await r.ReadAsync();
                highest = r.IsDBNull(0) ? 0m : r.GetDecimal(0); previousBidder = r.IsDBNull(1) ? null : r.GetInt32(1);
            }
            if (status != "OnAuction" || end <= DateTime.UtcNow) throw ServiceHelpers.Conflict("Наддава се само по активен търг преди крайния час.");
            if (owner == bidder) throw ServiceHelpers.Forbidden("Не можеш да наддаваш за собствен предмет.");
            // Старите стойности на офертите се пазят в лева; в интерфейса се въвеждат евро.
            var minimum = AuctionCurrency.MinimumBidInLevaAfter(highest == 0 ? null : highest);
            if (amountInLeva < minimum) throw ServiceHelpers.Conflict($"Новото наддаване трябва да е поне {AuctionCurrency.MinimumBidInEuroAfter(highest == 0 ? null : highest):0.00} €.");
            await using var add = new NpgsqlCommand("INSERT INTO dbo.Bids(ItemId,BidderId,Amount) VALUES(@item,@user,@amount)", db, tx);
            add.Parameters.AddWithValue("@item", itemId); add.Parameters.AddWithValue("@user", bidder); add.Parameters.AddWithValue("@amount", amountInLeva); await add.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch { await tx.RollbackAsync(); throw; }

        await notifications.CreateAsync(owner, bidder, itemId, "auction-bid", "Ново наддаване", "Има нова оферта за твоя предмет.");
        if (previousBidder is int previous && previous != bidder)
            await notifications.CreateAsync(previous, bidder, itemId, "auction-outbid", "Наддаването ти е надминато", "Вече има по-висока оферта за този предмет.");
        return new { itemId, amount = AuctionCurrency.ToEuro(amountInLeva), minimumNextBid = AuctionCurrency.MinimumBidInEuroAfter(amountInLeva) };
    }

    private static async Task<TradeDto> ReadTrade(int id, NpgsqlConnection db)
    {
        await using var cmd = new NpgsqlCommand("SELECT Id,WantedItemId,OfferedItemId,SenderId,Status,Note,CreatedAt FROM dbo.TradeOffers WHERE Id=@id", db); cmd.Parameters.AddWithValue("@id", id);
        await using var r = await cmd.ExecuteReaderAsync(); if (!await r.ReadAsync()) throw ServiceHelpers.NotFound();
        return new TradeDto(r.GetInt32(0),r.GetInt32(1),r.GetInt32(2),r.GetInt32(3),r.GetString(4),r.IsDBNull(5)?null:r.GetString(5),r.GetDateTime(6));
    }
}

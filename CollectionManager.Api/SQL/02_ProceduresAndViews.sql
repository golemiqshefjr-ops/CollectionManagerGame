USE CollectionManager;
GO
IF TYPE_ID(N'dbo.CategoryNameList') IS NULL EXEC(N'CREATE TYPE dbo.CategoryNameList AS TABLE(Name nvarchar(60) NOT NULL PRIMARY KEY)');
GO
CREATE OR ALTER PROCEDURE dbo.AcceptTradeOffer @TradeOfferId int, @OwnerUserId int
AS
BEGIN
  SET NOCOUNT ON; SET XACT_ABORT ON;
  BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Wanted int,@Offered int,@Sender int,@Recipient int;
    SELECT @Wanted=WantedItemId,@Offered=OfferedItemId,@Sender=SenderId,@Recipient=RecipientId
      FROM dbo.TradeOffers WITH(UPDLOCK,HOLDLOCK) WHERE Id=@TradeOfferId AND Status='Pending';
    IF @Wanted IS NULL BEGIN THROW 51000,'Предложението липсва или вече не е чакащо.',1; END;
    IF @Recipient<>@OwnerUserId BEGIN THROW 51001,'Само собственикът на желания предмет може да приеме размяната.',1; END;
    IF NOT EXISTS(SELECT 1 FROM dbo.Items WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Wanted AND OwnerId=@Recipient AND Status='ForTrade')
      OR NOT EXISTS(SELECT 1 FROM dbo.Items WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Offered AND OwnerId=@Sender AND Status='InCollection')
      BEGIN THROW 51000,'Предметите вече не отговарят на условията за размяна.',1; END;
    UPDATE dbo.Items SET OwnerId=@Sender,Status='Traded' WHERE Id=@Wanted;
    UPDATE dbo.Items SET OwnerId=@Recipient,Status='Traded' WHERE Id=@Offered;
    UPDATE dbo.TradeOffers SET Status='Accepted' WHERE Id=@TradeOfferId;
    UPDATE dbo.TradeOffers SET Status='Declined'
      WHERE Id<>@TradeOfferId AND Status='Pending' AND (WantedItemId IN(@Wanted,@Offered) OR OfferedItemId IN(@Wanted,@Offered));
    COMMIT;
  END TRY
  BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.CloseAuction @Categories dbo.CategoryNameList READONLY
AS
BEGIN
  SET NOCOUNT ON; SET XACT_ABORT ON; DECLARE @Item int,@Winner int,@Closed int=0;
  BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE due CURSOR LOCAL FAST_FORWARD FOR
      SELECT i.Id FROM dbo.Items i WITH(UPDLOCK,READPAST) JOIN dbo.Categories c ON c.Id=i.CategoryId JOIN @Categories allowed ON allowed.Name=c.Name
      WHERE i.Status='OnAuction' AND i.AuctionEndsAt<=SYSUTCDATETIME();
    OPEN due; FETCH NEXT FROM due INTO @Item;
    WHILE @@FETCH_STATUS=0
    BEGIN
      SET @Winner=NULL;
      SELECT TOP(1) @Winner=BidderId FROM dbo.Bids WHERE ItemId=@Item ORDER BY Amount DESC,CreatedAt ASC,Id ASC;
      IF @Winner IS NULL UPDATE dbo.Items SET Status='InCollection',AuctionEndsAt=NULL WHERE Id=@Item AND Status='OnAuction';
      ELSE UPDATE dbo.Items SET OwnerId=@Winner,Status='Sold',AuctionEndsAt=NULL WHERE Id=@Item AND Status='OnAuction';
      SET @Closed+=@@ROWCOUNT;
      FETCH NEXT FROM due INTO @Item;
    END
    CLOSE due; DEALLOCATE due; COMMIT; SELECT @Closed;
  END TRY
  BEGIN CATCH IF CURSOR_STATUS('local','due')>=0 CLOSE due; IF CURSOR_STATUS('local','due')>-3 DEALLOCATE due; IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER VIEW dbo.vw_AuctionLeaders
AS SELECT i.Id ItemId,i.Name,i.AuctionEndsAt,i.OwnerId,MAX(b.Amount) HighestBid,COUNT(b.Id) BidCount
FROM dbo.Items i LEFT JOIN dbo.Bids b ON b.ItemId=i.Id WHERE i.Status='OnAuction' GROUP BY i.Id,i.Name,i.AuctionEndsAt,i.OwnerId;
GO
CREATE OR ALTER VIEW dbo.vw_CollectionInventory
AS SELECT u.Id OwnerId,u.DisplayName,c.Name Category,i.Status,COUNT_BIG(*) ItemCount
FROM dbo.Items i JOIN dbo.Users u ON u.Id=i.OwnerId JOIN dbo.Categories c ON c.Id=i.CategoryId
GROUP BY u.Id,u.DisplayName,c.Name,i.Status;
GO


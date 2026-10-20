/* Run once in SQL Server Management Studio or sqlcmd. */
IF DB_ID(N'CollectionManager') IS NULL CREATE DATABASE CollectionManager;
GO
USE CollectionManager;
GO
IF OBJECT_ID('dbo.Messages','U') IS NULL
BEGIN
  CREATE TABLE dbo.Users(
    Id int IDENTITY PRIMARY KEY, Email nvarchar(180) NOT NULL UNIQUE, DisplayName nvarchar(80) NOT NULL,
    Role nvarchar(20) NOT NULL CONSTRAINT CK_Users_Role CHECK(Role IN ('Collector','Evaluator','Admin')),
    PasswordSalt varbinary(32) NOT NULL, PasswordHash varbinary(64) NOT NULL,
    Phone nvarchar(40) NULL, DeliveryAddress nvarchar(300) NULL, IsActive bit NOT NULL CONSTRAINT DF_Users_Active DEFAULT 1,
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Users_Created DEFAULT SYSUTCDATETIME());
  CREATE TABLE dbo.Categories(
    Id int IDENTITY PRIMARY KEY, Name nvarchar(60) NOT NULL UNIQUE,
    IsActive bit NOT NULL CONSTRAINT DF_Categories_Active DEFAULT 1);
  CREATE TABLE dbo.Items(
    Id int IDENTITY PRIMARY KEY, Name nvarchar(120) NOT NULL, CategoryId int NOT NULL REFERENCES dbo.Categories(Id),
    OwnerId int NOT NULL REFERENCES dbo.Users(Id), Description nvarchar(1200) NULL, Condition nvarchar(40) NULL,
    ImageUrl nvarchar(500) NULL, DeliveryAddress nvarchar(300) NULL, Phone nvarchar(40) NULL,
    Status nvarchar(24) NOT NULL CONSTRAINT CK_Items_Status CHECK(Status IN ('InCollection','ForTrade','Traded','PendingApproval','OnAuction','Sold')),
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Items_Created DEFAULT SYSUTCDATETIME(), AuctionEndsAt datetime2 NULL,
    CONSTRAINT CK_Items_AuctionDate CHECK((Status='OnAuction' AND AuctionEndsAt IS NOT NULL) OR Status<>'OnAuction'));
  CREATE INDEX IX_Items_Status_Category ON dbo.Items(Status,CategoryId);
  CREATE TABLE dbo.TradeOffers(
    Id int IDENTITY PRIMARY KEY, WantedItemId int NOT NULL REFERENCES dbo.Items(Id), OfferedItemId int NOT NULL REFERENCES dbo.Items(Id),
    SenderId int NOT NULL REFERENCES dbo.Users(Id), RecipientId int NOT NULL REFERENCES dbo.Users(Id), Note nvarchar(500) NULL,
    Status nvarchar(16) NOT NULL CONSTRAINT CK_Trade_Status CHECK(Status IN ('Pending','Accepted','Declined','Cancelled')),
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Trades_Created DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Trade_DifferentItems CHECK(WantedItemId<>OfferedItemId));
  CREATE INDEX IX_Trades_Pending_Items ON dbo.TradeOffers(Status,WantedItemId,OfferedItemId);
  CREATE TABLE dbo.Bids(
    Id int IDENTITY PRIMARY KEY, ItemId int NOT NULL REFERENCES dbo.Items(Id), BidderId int NOT NULL REFERENCES dbo.Users(Id),
    Amount decimal(12,2) NOT NULL CHECK(Amount>0), CreatedAt datetime2 NOT NULL CONSTRAINT DF_Bids_Created DEFAULT SYSUTCDATETIME());
  CREATE INDEX IX_Bids_Item_Amount ON dbo.Bids(ItemId,Amount DESC,CreatedAt);
  CREATE TABLE dbo.Messages(
    Id int IDENTITY PRIMARY KEY, SenderId int NOT NULL REFERENCES dbo.Users(Id), RecipientId int NOT NULL REFERENCES dbo.Users(Id),
    ItemId int NULL REFERENCES dbo.Items(Id), Body nvarchar(2000) NOT NULL, SentAt datetime2 NOT NULL CONSTRAINT DF_Messages_Sent DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Messages_Participants CHECK(SenderId<>RecipientId));
  CREATE INDEX IX_Messages_Conversation ON dbo.Messages(SenderId,RecipientId,SentAt);
END;
GO


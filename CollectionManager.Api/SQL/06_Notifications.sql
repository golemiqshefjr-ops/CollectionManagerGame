/* Run once against the CollectionManager database to enable persistent in-app notifications. */
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Notifications(
    Id int IDENTITY PRIMARY KEY,
    RecipientId int NOT NULL REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    ActorId int NULL,
    ItemId int NULL,
    Type nvarchar(32) NOT NULL,
    Title nvarchar(120) NOT NULL,
    Body nvarchar(300) NOT NULL,
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Notifications_Created DEFAULT SYSUTCDATETIME(),
    IsRead bit NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT 0
  );
  CREATE INDEX IX_Notifications_Recipient_Read_Created ON dbo.Notifications(RecipientId,IsRead,CreatedAt DESC);
END;
GO


USE CollectionManager;
GO
/* Demo password hashes use PBKDF2-HMAC-SHA256, 210,000 rounds, 16-byte salt, 32-byte key.
   Change the sample passwords after first local run. Admin cannot be registered through the API. */
IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE Email='admin@kolektsiya.local')
  INSERT dbo.Users(Email,DisplayName,Role,PasswordSalt,PasswordHash) VALUES
  ('admin@kolektsiya.local',N'Администратор','Admin',0xCB39B4ABA79C98504DD4100B7361E085,0xB4E3434A6B61AD037D6D0D50DC0070C7451DB7276D6AE872861B200B96A5E3CD);
IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE Email='ocenitel@kolektsiya.local')
  INSERT dbo.Users(Email,DisplayName,Role,PasswordSalt,PasswordHash) VALUES
  ('ocenitel@kolektsiya.local',N'Оценител','Evaluator',0x72B7AD1F1CC78B0F5441DDE9116FF037,0x72C0C008AD5480E9A813ADC1B00AD055F1D35D79E4D2E7D5F9A37CFD042C7901);
IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE Email='boris@kolektsiya.local')
  INSERT dbo.Users(Email,DisplayName,Role,PasswordSalt,PasswordHash) VALUES
  ('boris@kolektsiya.local',N'Борис — демо колекционер','Collector',0xF89F5CF7B584A046823E600F1ABB6741,0x528EE8D2E4D2517D59BD12696D8E3C920D0E19A16C8B1046F12EF14F0BB88C5F);
IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE Email='mira@kolektsiya.local')
  INSERT dbo.Users(Email,DisplayName,Role,PasswordSalt,PasswordHash) VALUES
  ('mira@kolektsiya.local',N'Мира — демо колекционер','Collector',0x515D56A5668A65AE702ED1C3F60F2043,0x8CD71A8186A6A46232C734BB34FA08BBBA6C7752F1DFFF0AC88A115AE0636D4C);
IF NOT EXISTS(SELECT 1 FROM dbo.Categories)
  INSERT dbo.Categories(Name) VALUES(N'Монети'),(N'Марки'),(N'Картички'),(N'Винилови плочи'),(N'Игри и конзоли'),(N'Фигурки');
DECLARE @Boris int=(SELECT Id FROM dbo.Users WHERE Email='boris@kolektsiya.local');
DECLARE @Mira int=(SELECT Id FROM dbo.Users WHERE Email='mira@kolektsiya.local');
IF NOT EXISTS(SELECT 1 FROM dbo.Items)
BEGIN
  INSERT dbo.Items(Name,CategoryId,OwnerId,Description,Condition,DeliveryAddress,Phone,Status)
    SELECT N'Game Boy Pocket · прозрачен',Id,@Boris,N'Работеща конзола от 1997 г. Запазени бутони и оригинален капак.',N'Много добро',N'София, център',N'+359 888 123 456','InCollection' FROM dbo.Categories WHERE Name=N'Игри и конзоли';
  INSERT dbo.Items(Name,CategoryId,OwnerId,Description,Condition,DeliveryAddress,Phone,Status)
    SELECT N'Pokémon Base Set · 1st Edition',Id,@Boris,N'Карти от базовото издание, съхранявани в протектори.',N'Отлично',N'София, център',N'+359 888 123 456','InCollection' FROM dbo.Categories WHERE Name=N'Картички';
  INSERT dbo.Items(Name,CategoryId,OwnerId,Description,Condition,DeliveryAddress,Phone,Status)
    SELECT N'Монета 5 стотинки · 1887',Id,@Mira,N'Княжество България. Рядка година с характерна патина.',N'Много добро',N'Пловдив',N'+359 887 555 201','ForTrade' FROM dbo.Categories WHERE Name=N'Монети';
  INSERT dbo.Items(Name,CategoryId,OwnerId,Description,Condition,DeliveryAddress,Phone,Status)
    SELECT N'Пощенска картичка · Цар Борис III',Id,@Mira,N'Оригинална картичка с пощенско клеймо от 1930-те години.',N'Добро',N'Пловдив',N'+359 887 555 201','PendingApproval' FROM dbo.Categories WHERE Name=N'Картички';
  INSERT dbo.Items(Name,CategoryId,OwnerId,Description,Condition,DeliveryAddress,Phone,Status,AuctionEndsAt)
    SELECT N'Pink Floyd · The Dark Side of the Moon',Id,@Mira,N'Първо британско издание с вложки и постери.',N'VG+',N'Пловдив',N'+359 887 555 201','OnAuction',DATEADD(day,5,SYSUTCDATETIME()) FROM dbo.Categories WHERE Name=N'Винилови плочи';
END;
/* Resolve demo records by stable ownership/status keys rather than localized names. */
DECLARE @wanted int=(SELECT TOP(1) Id FROM dbo.Items WHERE OwnerId=@Mira AND Status='ForTrade' ORDER BY Id);
DECLARE @offered int=(SELECT TOP(1) Id FROM dbo.Items WHERE OwnerId=@Boris AND Status='InCollection' ORDER BY Id);
DECLARE @auction int=(SELECT TOP(1) Id FROM dbo.Items WHERE OwnerId=@Mira AND Status='OnAuction' ORDER BY Id);
IF @wanted IS NOT NULL AND @offered IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.TradeOffers WHERE WantedItemId=@wanted AND OfferedItemId=@offered AND SenderId=@Boris)
  INSERT dbo.TradeOffers(WantedItemId,OfferedItemId,SenderId,RecipientId,Note,Status) VALUES(@wanted,@offered,@Boris,@Mira,N'Мога да добавя и защитен калъф за конзолата.','Pending');
IF @auction IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Bids WHERE ItemId=@auction AND BidderId=@Boris)
  INSERT dbo.Bids(ItemId,BidderId,Amount) VALUES(@auction,@Boris,185.00);
GO


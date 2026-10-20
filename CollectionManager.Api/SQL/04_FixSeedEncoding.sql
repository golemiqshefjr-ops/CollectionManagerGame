/* Repair demo text inserted with the wrong client code page; preserve all IDs and user data. */
USE CollectionManager;
GO
UPDATE dbo.Categories SET Name=CASE Id
  WHEN 1 THEN N'Монети' WHEN 2 THEN N'Марки' WHEN 3 THEN N'Картички'
  WHEN 4 THEN N'Винилови плочи' WHEN 5 THEN N'Игри и конзоли' WHEN 6 THEN N'Фигурки' END
WHERE Id BETWEEN 1 AND 6;
UPDATE dbo.Users SET DisplayName=CASE Email
  WHEN N'admin@kolektsiya.local' THEN N'Администратор'
  WHEN N'ocenitel@kolektsiya.local' THEN N'Оценител'
  WHEN N'boris@kolektsiya.local' THEN N'Борис — демо колекционер'
  WHEN N'mira@kolektsiya.local' THEN N'Мира — демо колекционер' END
WHERE Email IN(N'admin@kolektsiya.local',N'ocenitel@kolektsiya.local',N'boris@kolektsiya.local',N'mira@kolektsiya.local');
UPDATE dbo.Items SET Name=CASE Id
  WHEN 1 THEN N'Game Boy Pocket · прозрачен'
  WHEN 2 THEN N'Pokémon Base Set · 1st Edition'
  WHEN 3 THEN N'Монета 5 стотинки · 1887'
  WHEN 4 THEN N'Пощенска картичка · Цар Борис III'
  WHEN 5 THEN N'Pink Floyd · The Dark Side of the Moon' END,
  Description=CASE Id
  WHEN 1 THEN N'Работеща конзола от 1997 г. Запазени бутони и оригинален капак.'
  WHEN 2 THEN N'Карти от базовото издание, съхранявани в протектори.'
  WHEN 3 THEN N'Княжество България. Рядка година с характерна патина.'
  WHEN 4 THEN N'Оригинална картичка с пощенско клеймо от 1930-те години.'
  WHEN 5 THEN N'Първо британско издание с вложки и постери.' END,
  Condition=CASE Id WHEN 1 THEN N'Много добро' WHEN 2 THEN N'Отлично' WHEN 3 THEN N'Много добро' WHEN 4 THEN N'Добро' WHEN 5 THEN N'VG+' END,
  DeliveryAddress=CASE Id WHEN 1 THEN N'София, център' WHEN 2 THEN N'София, център' WHEN 3 THEN N'Пловдив' WHEN 4 THEN N'Пловдив' WHEN 5 THEN N'Пловдив' END
WHERE Id BETWEEN 1 AND 5;
UPDATE dbo.TradeOffers SET Note=N'Мога да добавя и защитен калъф за конзолата.' WHERE Id=1;
GO


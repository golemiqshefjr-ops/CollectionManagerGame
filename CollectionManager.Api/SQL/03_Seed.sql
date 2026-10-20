-- Optional demo data. Run only when you want the demo accounts and sample items.
INSERT INTO dbo.users(email,displayname,role,passwordsalt,passwordhash) VALUES
('admin@kolektsiya.local','Администратор','Admin',decode('cb39b4aba79c98504dd4100b7361e085','hex'),decode('b4e3434a6b61ad037d6d0d50dc0070c7451db7276d6ae872861b200b96a5e3cd','hex')),
('ocenitel@kolektsiya.local','Оценител','Evaluator',decode('72b7ad1f1cc78b0f5441dde9116ff037','hex'),decode('72c0c008ad5480e9a813adc1b00ad055f1d35d79e4d2e7d5f9a37cfd042c7901','hex')),
('boris@kolektsiya.local','Борис — демо колекционер','Collector',decode('f89f5cf7b584a046823e600f1abb6741','hex'),decode('528ee8d2e4d2517d59bd12696d8e3c920d0e19a16c8b1046f12ef14f0bb88c5f','hex')),
('mira@kolektsiya.local','Мира — демо колекционер','Collector',decode('515d56a5668a65ae702ed1c3f60f2043','hex'),decode('8cd71a8186a6a46232c734bb34fa08bbba6c7752f1dfff0ac88a115ae0636d4c','hex'))
ON CONFLICT (email) DO NOTHING;

INSERT INTO dbo.items(name,categoryid,ownerid,description,condition,deliveryaddress,phone,status)
SELECT 'Game Boy Pocket · прозрачен',c.id,u.id,'Работеща конзола от 1997 г. Запазени бутони и оригинален капак.','Много добро','София, център','+359 888 123 456','InCollection'
FROM dbo.users u JOIN dbo.categories c ON c.name='Игри и конзоли'
WHERE u.email='boris@kolektsiya.local' AND NOT EXISTS(SELECT 1 FROM dbo.items i WHERE i.name='Game Boy Pocket · прозрачен' AND i.ownerid=u.id);

INSERT INTO dbo.items(name,categoryid,ownerid,description,condition,deliveryaddress,phone,status)
SELECT 'Pokémon Base Set · 1st Edition',c.id,u.id,'Карти от базовото издание, съхранявани в протектори.','Отлично','София, център','+359 888 123 456','InCollection'
FROM dbo.users u JOIN dbo.categories c ON c.name='Картички'
WHERE u.email='boris@kolektsiya.local' AND NOT EXISTS(SELECT 1 FROM dbo.items i WHERE i.name='Pokémon Base Set · 1st Edition' AND i.ownerid=u.id);

INSERT INTO dbo.items(name,categoryid,ownerid,description,condition,deliveryaddress,phone,status)
SELECT 'Монета 5 стотинки · 1887',c.id,u.id,'Княжество България. Рядка година с характерна патина.','Много добро','Пловдив','+359 887 555 201','ForTrade'
FROM dbo.users u JOIN dbo.categories c ON c.name='Монети'
WHERE u.email='mira@kolektsiya.local' AND NOT EXISTS(SELECT 1 FROM dbo.items i WHERE i.name='Монета 5 стотинки · 1887' AND i.ownerid=u.id);

INSERT INTO dbo.items(name,categoryid,ownerid,description,condition,deliveryaddress,phone,status)
SELECT 'Пощенска картичка · Цар Борис III',c.id,u.id,'Оригинална картичка с пощенско клеймо от 1930-те години.','Добро','Пловдив','+359 887 555 201','PendingApproval'
FROM dbo.users u JOIN dbo.categories c ON c.name='Картички'
WHERE u.email='mira@kolektsiya.local' AND NOT EXISTS(SELECT 1 FROM dbo.items i WHERE i.name='Пощенска картичка · Цар Борис III' AND i.ownerid=u.id);

INSERT INTO dbo.items(name,categoryid,ownerid,description,condition,deliveryaddress,phone,status,auctionendsat)
SELECT 'Pink Floyd · The Dark Side of the Moon',c.id,u.id,'Първо британско издание с вложки и постери.','VG+','Пловдив','+359 887 555 201','OnAuction',CURRENT_TIMESTAMP + INTERVAL '5 days'
FROM dbo.users u JOIN dbo.categories c ON c.name='Винилови плочи'
WHERE u.email='mira@kolektsiya.local' AND NOT EXISTS(SELECT 1 FROM dbo.items i WHERE i.name='Pink Floyd · The Dark Side of the Moon' AND i.ownerid=u.id);

INSERT INTO dbo.tradeoffers(wanteditemid,offereditemid,senderid,recipientid,note,status)
SELECT wanted.id,offered.id,sender.id,recipient.id,'Мога да добавя и защитен калъф за конзолата.','Pending'
FROM dbo.users sender,dbo.users recipient,dbo.items wanted,dbo.items offered
WHERE sender.email='boris@kolektsiya.local' AND recipient.email='mira@kolektsiya.local'
  AND wanted.ownerid=recipient.id AND wanted.status='ForTrade' AND offered.ownerid=sender.id AND offered.status='InCollection'
  AND NOT EXISTS(SELECT 1 FROM dbo.tradeoffers t WHERE t.wanteditemid=wanted.id AND t.offereditemid=offered.id AND t.senderid=sender.id);

INSERT INTO dbo.bids(itemid,bidderid,amount)
SELECT i.id,u.id,185.00 FROM dbo.items i JOIN dbo.users owner ON owner.id=i.ownerid CROSS JOIN dbo.users u
WHERE i.name='Pink Floyd · The Dark Side of the Moon' AND owner.email='mira@kolektsiya.local' AND u.email='boris@kolektsiya.local'
  AND NOT EXISTS(SELECT 1 FROM dbo.bids b WHERE b.itemid=i.id AND b.bidderid=u.id);

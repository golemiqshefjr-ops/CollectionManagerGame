-- PostgreSQL equivalents of the reporting views previously implemented in T-SQL.
CREATE OR REPLACE VIEW dbo.vw_auctionleaders AS
SELECT i.id AS itemid,i.name,i.auctionendsat,i.ownerid,MAX(b.amount) AS highestbid,COUNT(b.id) AS bidcount
FROM dbo.items i
LEFT JOIN dbo.bids b ON b.itemid=i.id
WHERE i.status='OnAuction'
GROUP BY i.id,i.name,i.auctionendsat,i.ownerid;

CREATE OR REPLACE VIEW dbo.vw_collectioninventory AS
SELECT u.id AS ownerid,u.displayname,c.name AS category,i.status,COUNT(*) AS itemcount
FROM dbo.items i
JOIN dbo.users u ON u.id=i.ownerid
JOIN dbo.categories c ON c.id=i.categoryid
GROUP BY u.id,u.displayname,c.name,i.status;

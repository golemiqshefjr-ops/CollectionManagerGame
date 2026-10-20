/* Adds public collector profile details. Safe to run again after an interrupted setup. */
USE CollectionManager;
GO
IF COL_LENGTH('dbo.Users','Bio') IS NULL ALTER TABLE dbo.Users ADD Bio nvarchar(500) NULL;
IF COL_LENGTH('dbo.Users','AvatarUrl') IS NULL ALTER TABLE dbo.Users ADD AvatarUrl nvarchar(500) NULL;
IF COL_LENGTH('dbo.Users','Location') IS NULL ALTER TABLE dbo.Users ADD Location nvarchar(80) NULL;
IF COL_LENGTH('dbo.Users','Website') IS NULL ALTER TABLE dbo.Users ADD Website nvarchar(200) NULL;
GO


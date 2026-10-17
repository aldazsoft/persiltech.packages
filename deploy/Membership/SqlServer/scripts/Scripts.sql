USE [Persiltech_MembershipSample]
GO

SELECT * FROM [dbo].[AspNetUsers]
SELECT * FROM [dbo].[AspNetUserRoles]
SELECT * FROM [dbo].[AspNetUserClaims]
SELECT * FROM [dbo].[AspNetUserLogins]
SELECT * FROM [dbo].[AspNetUserTokens]

SELECT * FROM [dbo].[AspNetRoles]
SELECT * FROM [dbo].[AspNetRoleClaims]

-- Open ID
SELECT * FROM [dbo].[OpenIddictApplications]
SELECT * FROM [dbo].[OpenIddictAuthorizations]
SELECT * FROM [dbo].[OpenIddictScopes]
SELECT * FROM [dbo].[OpenIddictTokens]

----------------------

TRUNCATE TABLE [dbo].[AspNetUserRoles]
DELETE FROM [dbo].[AspNetUsers]
DELETE FROM [dbo].[AspNetRoles]
DELETE FROM [dbo].[OpenIddictApplications]

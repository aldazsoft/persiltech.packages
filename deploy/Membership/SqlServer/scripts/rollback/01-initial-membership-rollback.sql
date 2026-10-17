BEGIN TRANSACTION;
DROP TABLE [AspNetRoleClaims];

DROP TABLE [AspNetUserClaims];

DROP TABLE [AspNetUserLogins];

DROP TABLE [AspNetUserRoles];

DROP TABLE [AspNetUserTokens];

DROP TABLE [AspNetRoles];

DROP TABLE [AspNetUsers];

DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20260906151233_InitialMembership';

COMMIT;
GO


BEGIN TRANSACTION;
DROP TABLE [OpenIddictScopes];

DROP TABLE [OpenIddictTokens];

DROP TABLE [OpenIddictAuthorizations];

DROP TABLE [OpenIddictApplications];

DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20260906151734_InitialOAuth';

COMMIT;
GO


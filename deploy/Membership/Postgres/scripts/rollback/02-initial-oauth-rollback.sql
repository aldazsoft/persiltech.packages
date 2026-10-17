START TRANSACTION;
DROP TABLE "OpenIddictScopes";

DROP TABLE "OpenIddictTokens";

DROP TABLE "OpenIddictAuthorizations";

DROP TABLE "OpenIddictApplications";

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20261017111647_InitialOAuth';

COMMIT;


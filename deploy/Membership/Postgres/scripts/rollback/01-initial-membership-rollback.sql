START TRANSACTION;
DROP TABLE "AspNetRoleClaims";

DROP TABLE "AspNetUserClaims";

DROP TABLE "AspNetUserLogins";

DROP TABLE "AspNetUserRoles";

DROP TABLE "AspNetUserTokens";

DROP TABLE "MembershipRefreshTokens";

DROP TABLE "AspNetRoles";

DROP TABLE "AspNetUsers";

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20261017111644_InitialMembership';

COMMIT;


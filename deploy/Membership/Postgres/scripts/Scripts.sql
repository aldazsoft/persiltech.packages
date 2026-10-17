-- Ejecutar en DBeaver conectado a Persiltech_MembershipSample.
-- Los nombres van entre comillas: PostgreSQL, si no, los pasa a minúsculas.

SELECT * FROM "AspNetUsers";
SELECT * FROM "AspNetUserRoles";
SELECT * FROM "AspNetUserClaims";
SELECT * FROM "AspNetUserLogins";
SELECT * FROM "AspNetUserTokens";

SELECT * FROM "AspNetRoles";
SELECT * FROM "AspNetRoleClaims";

-- Open ID
SELECT * FROM "OpenIddictApplications";
SELECT * FROM "OpenIddictAuthorizations";
SELECT * FROM "OpenIddictScopes";
SELECT * FROM "OpenIddictTokens";

----------------------

TRUNCATE TABLE "AspNetUserRoles";
DELETE FROM "AspNetUsers";
DELETE FROM "AspNetRoles";
DELETE FROM "OpenIddictApplications";

# Persiltech

Monorepo de los paquetes NuGet de Persiltech. Este documento recoge los comandos de
Entity Framework Core para las migraciones de `Persiltech.Membership` y
`Persiltech.Membership.OAuth`.

Los comandos se ejecutan **desde la raíz del repositorio**, y la herramienta `dotnet ef`
es local: si aún no está restaurada, `dotnet tool restore` la instala desde
`.config/dotnet-tools.json`.

El proyecto anfitrión de ambos contextos es `samples/Persiltech.Membership.Sample`, que
es a la vez el proyecto y el proyecto de arranque, así que no hace falta declarar
`--startup-project`.

> `samples/Persiltech.Membership.Sample` declara **dos** `DbContext`:
> `MembershipDbContext` y `MembershipOAuthDbContext`. Por eso **`--context` es
> obligatorio** en todos los comandos: sin él, `dotnet ef` aborta con
> _"More than one DbContext was found"_.

## Persiltech.Membership

Contexto `MembershipDbContext`, con las tablas de ASP.NET Core Identity. Sus migraciones
viven en `Migrations/`, que es la carpeta por defecto.

### Crear migración

```powershell
dotnet ef migrations add InitialMembership `
  --project samples/Persiltech.Membership.Sample/Persiltech.Membership.Sample.csproj `
  --context MembershipDbContext
```

### Generar el script de migración

`--idempotent` envuelve cada paso en una comprobación contra `__EFMigrationsHistory`, de
modo que el script puede aplicarse sobre una base de datos en cualquier estado.

```powershell
dotnet ef migrations script --idempotent `
  --project samples/Persiltech.Membership.Sample/Persiltech.Membership.Sample.csproj `
  --context MembershipDbContext `
  --output deploy/Membership/scripts/01-initial-membership.sql
```

### Generar el script de rollback

El comando toma **desde** y **hasta**: se parte de la última migración aplicada y se
llega a `0`, que es la base de datos vacía.

```powershell
dotnet ef migrations script InitialMembership 0 `
  --project samples/Persiltech.Membership.Sample/Persiltech.Membership.Sample.csproj `
  --context MembershipDbContext `
  --output deploy/Membership/scripts/rollback/01-initial-membership-rollback.sql
```

## Persiltech.Membership.OAuth

Contexto `MembershipOAuthDbContext`, con las tablas de OpenIddict. Sus migraciones se
guardan aparte, en `Migrations/MembershipOAuthDb/`, para no mezclarse con las de
Identity; de ahí el `--output-dir` al crearlas.

### Crear migración

```powershell
dotnet ef migrations add InitialOAuth `
  --project samples/Persiltech.Membership.Sample/Persiltech.Membership.Sample.csproj `
  --context MembershipOAuthDbContext `
  --output-dir Migrations/MembershipOAuthDb
```

### Generar el script de migración

```powershell
dotnet ef migrations script --idempotent `
  --project samples/Persiltech.Membership.Sample/Persiltech.Membership.Sample.csproj `
  --context MembershipOAuthDbContext `
  --output deploy/Membership/scripts/02-initial-oauth.sql
```

### Generar el script de rollback

```powershell
dotnet ef migrations script InitialOAuth 0 `
  --project samples/Persiltech.Membership.Sample/Persiltech.Membership.Sample.csproj `
  --context MembershipOAuthDbContext `
  --output deploy/Membership/scripts/rollback/02-initial-oauth-rollback.sql
```

## Orden de aplicación

Los dos contextos son independientes y no comparten tablas, así que el orden entre ellos
da igual. Dentro de cada uno, el rollback se aplica en sentido inverso al despliegue.

## Listar las migraciones existentes

```powershell
dotnet ef migrations list `
  --project samples/Persiltech.Membership.Sample/Persiltech.Membership.Sample.csproj `
  --context MembershipDbContext
```

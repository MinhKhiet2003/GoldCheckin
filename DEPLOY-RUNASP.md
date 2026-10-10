# Deploy GoldCheckin to ASP.NET hosting (RunASP)

## 1. Publish

The project is `Base/Base.csproj` (ASP.NET Core 10). The current Release build succeeds. In the hosting Build & Publish page, select `Base\Base.csproj` and publish after pulling the latest `main` branch.

## 2. App-pool environment variables

In the environment-variable settings for the `goldcheckin.runasp.net` application pool, configure these exact names (double underscore represents `:` in ASP.NET Core):

- `ASPNETCORE_ENVIRONMENT` = `Production`
- `ConnectionStrings__DefaultConnection` = the database provider's **Local access** connection string, adapted to Npgsql format:
  `Host=<internal-host>;Port=5432;Database=<database>;Username=<database-user>;Password=<database-password>;SSL Mode=Prefer`
- `Jwt__Key` = a newly generated random secret with at least 32 UTF-8 bytes. Do not use a sample value or commit it.
- `Jwt__Issuer` = `Base.Api`
- `Jwt__Audience` = `Base.Client`

Add these only if the corresponding features are configured:

- `Gold__FaceEnrollmentUrl` and `Gold__FaceVerificationUrl` = reachable HTTPS CompreFace API URLs from the hosting environment
- `Gold__FaceApiKey` = Recognition service API key
- `Mail__Host`, `Mail__Port`, `Mail__UseSsl`, `Mail__Username`, `Mail__Password`, `Mail__FromEmail`, `Mail__FromName`, `Mail__PasswordResetUrl`

After saving environment variables, restart the app pool/site, then run Build & Publish again.

## 3. Database access

The hosting provider's **Local access** hostname is available only to applications running in that provider's network. A connection test from a local PC to that hostname can time out even if the hosted site can reach it. For local tools such as Navicat, use the database's separate **Remote access** host/settings and allow-list your public IP, if the provider supports it. Do not expose a database password in source control.

The database currently reports zero tables. After the app starts, apply EF migrations against the production database using a secure migration connection, or run the provider-supported migration process. At minimum this commit adds migration `20261006030000_FaceEnrollment`; the app code does not auto-run migrations at startup.

## 4. Diagnose HTTP 500.30

In the hosting panel, enable ASP.NET Core stdout/startup logging for the app, recycle/restart it, open the site once, and inspect the generated log file under the configured stdout-log directory. Disable stdout logging after diagnosis and remove old logs because they can contain sensitive connection details.

Check first for the explicit startup error requiring `Jwt:Key` to be at least 32 bytes. If that passes, inspect the next exception for missing/malformed database connection settings, missing runtime, or provider-specific startup errors. The browser's generic 500.30 page does not show the root exception.

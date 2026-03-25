# LogiTrack

LogiTrack is a .NET 8 Web API designed to manage inventory and order lifecycles for small to medium-sized logistics workflows. It provides authentication via ASP.NET Core Identity, EF Core-backed persistence using SQLite (file-based), and middleware for request correlation and robust error and request logging. The project includes API controllers for inventory and orders, EF Core migrations for schema management, and configuration options to simplify local development and production deployment.

## Quick overview

- Framework: .NET 8 (net8.0)
- Application type: ASP.NET Core Web API
- Persistence: Entity Framework Core using SQLite (file-based)
- Authentication: ASP.NET Core Identity (configurable)
- Migrations: EF Core migrations in the `Migrations/` folder
- Logs: file-based runtime logs under the `logs/` directory

## Features

- User authentication and identity integration
- Inventory management (CRUD for inventory items)
- Order creation and tracking
- EF Core migrations using SQLite
- Request/error logging and middleware for correlation and diagnostics

## Prerequisites

- Install the [.NET 8 SDK](https://dotnet.microsoft.com/download)
- (Optional) Install EF Core tools for migrations: `dotnet tool install --global dotnet-ef`

## Run (development)

Open a terminal in the project root and run:

```powershell
dotnet restore; dotnet run
```

Notes:

- Configuration is read from `appsettings.json` and `appsettings.Development.json` by default.
- On first run, the database will be initialized when migrations are applied.
 
 - Default HTTP port: 5000 (Kestrel is configured to listen on `http://*:5000` by default).
 - Default HTTPS port (development): 5001. To run HTTPS locally, ensure the .NET developer certificate is trusted:

```powershell
dotnet dev-certs https --trust
```

You can also configure Kestrel endpoints in `appsettings.json` or via environment variables.

## Build

Build the project:

```powershell
dotnet build
```

## Configuration

- App configuration files: `appsettings.json`, `appsettings.Development.json`.
- Connection string name: `Default` (when present). If not provided the app will use the configured SQLite file.


## Security notes

- Treat log files in `logs/` as potentially sensitive — they may contain IPs, usernames, or other diagnostics.
- This project is intended for learning and local development; review authentication and secrets before using in production.

### Secrets

- Provide required secrets via `dotnet user-secrets` (for local development) or environment variables in CI/production rather than embedding them in code.

Examples (local development with user-secrets):

```powershell
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "<your-jwt-secret>"
dotnet user-secrets set "Identity:PasswordKey" "<your-password-pepper>"
```

Or set environment variables (PowerShell):

```powershell
$env:Jwt__Key = "<your-jwt-secret>"
$env:LOGITRACK_PASSWORD_KEY = "<your-password-pepper>"
```

- If a secret was ever pushed to a public repository, rotate the secret immediately and update deployed environments.

### Swagger / OpenAPI

- The project registers Swagger and exposes the OpenAPI UI. When the app runs locally the Swagger UI is mounted at the application root and the OpenAPI JSON is available at `/swagger/v1/swagger.json`.
- To view the API documentation open the app URL in a browser (for example `http://localhost:5000/` or the port shown when running `dotnet run`) and the Swagger UI will load.
- The generated OpenAPI spec can be used for client generation, testing, or importing into tools such as Postman.

## Development tips

- Apply migrations locally with:

```powershell
dotnet ef database update
```

- To reset the local database during development, stop the app and remove the configured database file or run a fresh migration.

## Contributing

If you'd like to suggest improvements, open an issue or submit a pull request describing the change.

## Folder/File structure

```
└── 📁LogiTrack
    └── 📁Controllers
        ├── AuthController.cs
        ├── InventoryController.cs
        ├── OrderController.cs
    └── 📁Data
        ├── LogiTrackContext.cs
    └── 📁logs
    └── 📁Middleware
        ├── CorrelationIdMiddleware.cs
        ├── ErrorHandlingMiddleware.cs
        ├── RequestLoggingMiddleware.cs
    └── 📁Migrations
    └── 📁Models
        └── 📁Auth
            ├── LoginModel.cs
            ├── RegisterModel.cs
            ├── Roles.cs
        ├── ApplicationUser.cs
        ├── InventoryItem.cs
        ├── Order.cs
        ├── SessionInfo.cs
    └── 📁Properties
        ├── launchSettings.json
    └── 📁Utilities
        ├── CacheKeys.cs
        ├── CacheOptionsFactory.cs
    ├── appsettings.Development.json
    ├── appsettings.json
    ├── LogiTrack.csproj
    ├── LogiTrack.sln
    ├── Program.cs
    ├── README.md
    └── tests.http
```

---

## License

This project is available under the MIT License.

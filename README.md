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

- Identity configuration: For testing the project includes a placeholder password-hash key in the application configuration. The test key is stored in [appsettings.json](appsettings.json) under the `Identity:PasswordKey` configuration path; do not commit production secrets to source control.
- Recommendation: In production or shared environments, provide the password-hash key via an environment variable (for example `LOGITRACK_PASSWORD_KEY`) and read it in code when available. A minimal pattern:

```csharp
var passwordKey = Configuration["Identity:PasswordKey"] ?? Environment.GetEnvironmentVariable("LOGITRACK_PASSWORD_KEY");
```

- Secure storage: Use environment variables, user secrets (for local development), or a secret store (Azure Key Vault, AWS Secrets Manager, etc.) for production secrets and rotate them regularly.

## Project structure highlights

- `Program.cs` — application startup, DI, and middleware pipeline
- `Data/LogiTrackContext.cs` — EF Core DbContext
- `Controllers/` — `AuthController`, `InventoryController`, `OrderController`
- `Models/` — domain models: `Order`, `InventoryItem`, `ApplicationUser`
- `Middleware/` — request logging, error handling, correlation id middleware
- `Migrations/` — EF Core migrations

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
    └── README.md
```

---

## License

This project is available under the MIT License.

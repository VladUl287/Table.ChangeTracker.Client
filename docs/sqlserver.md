# SQL Server usage

[![NuGet Status](https://img.shields.io/nuget/v/ChangeTracker.SqlServer.svg?label=ChangeTracker.SqlServer)](https://www.nuget.org/packages/ChangeTracker.SqlServer/)

## Implementation

For SQL Server used [dm_db_index_usage_stats](https://learn.microsoft.com/en-us/sql/relational-databases/system-dynamic-management-views/sys-dm-db-index-usage-stats-transact-sql?view=sql-server-ver17) or [change tracking feature](https://learn.microsoft.com/en-us/sql/relational-databases/track-changes/enable-and-disable-change-tracking-sql-server?view=sql-server-ver17) depending on specifciation.

## Registration

`TrackingMode.DbIndexUsageStats` is default and can be not specified

```cs
var builder = WebApplication.CreateBuilder();
{
    builder.Services
        .AddTracker()
        .AddSqlServerProvider("my-sql-provider", "Data Source=localhost,1433;User ID=user;Password=password;Database=database;")
        .AddSqlServerProvider("my-sql-provider", "Data Source=localhost,1433;User ID=user;Password=password;Database=database;", TrackingMode.DbIndexUsageStats)
        .AddSqlServerProvider("my-sql-provider", "Data Source=localhost,1433;User ID=user;Password=password;Database=database;", TrackingMode.ChangeTracking)
        .AddSqlServerProvider<DatabaseContext>()
        .AddSqlServerProvider<DatabaseContext>(TrackingMode.DbIndexUsageStats)
        .AddSqlServerProvider<DatabaseContext>(TrackingMode.ChangeTracking)
        .AddSqlServerProvider<DatabaseContext>("my-sql-provider")
        .AddSqlServerProvider<DatabaseContext>("my-sql-provider", TrackingMode.DbIndexUsageStats)
        .AddSqlServerProvider<DatabaseContext>("my-sql-provider", TrackingMode.ChangeTracking);
}
```

[snippet source](../Tracker.Npgsql/Extensions/ServiceCollectionExtensions.cs)

## Source Providers

All default source provider logic incapsulated in [SqlServerIndexUsageOperations](../Tracker.SqlServer/Services/SqlServerIndexUsageOperations.cs) or [SqlServerChangeTrackingOperations](../Tracker.SqlServer/Services/SqlServerChangeTrackingOperations.cs) classes.
Providers resolved by [DefaultProviderResolver](../Tracker.AspNet/Services//DefaultProviderResolver.cs)

All source providers must include an `Id` field, which is used to register them as keyed dependency injection services. For multi-database support, they must also implement the ISourceProvider interface. When using `.AddSqlServerProvider<DatabaseContext>()` without a specified `Id`, the database context's full type name will serve as the source provider `Id`:

```cs
typeof(DatabaseContext).FullName
```

### Selection a Source Provider for change tracking

* **Default**: If no provider is specified, the first registered provider is used.

```cs
[Track()]
[Track(tables: ["roles"])]

//middlewares
app.UseTracker();
app.UseTracker((options) =>
{
    options.Tables = ["roles"];
});

//minimal APIs
app.MapGet("roles", () => { })
    .WithTracking();
app.MapGet("roles", () => { })
    .WithTracking(options =>
    {
        options.Tables = ["roles"];
    });
```

* **By Type**: Specifying a DbContext type resolves a provider registered for it.

```cs
[Track<DatabaseContext>()]
[Track<DatabaseContext>(tables: ["roles"])]

//middlewares
app.UseTracker<DatabaseContext>((options) =>
{
    options.Entities = [typeof(Role)];
});

//minimal APIs
app.MapGet("roles", () => { })
    .WithTracking<RouteHandlerBuilder, DatabaseContext>(options =>
    {
        options.Entities = [typeof(Role)];
    });
app.MapGroup("roles")
    .WithTracking<RouteGroupBuilder, DatabaseContext>(options => { })
    .MapGet("all", () => { });
```

* **By Id**: A provider registered with a specific Id can be referenced directly.

```cs
[Track(providerId: "my-sql-provider", tables: ["roles"])]
[Track<DatabaseContext>(**: "my-sql-provider", entities: [typeof(Role)])]

//middlewares
app.UseTracker<DatabaseContext>((options) =>
{
    options.ProviderId = "my-sql-provider";
    options.Entities = [typeof(Role)];
});

//minimal APIs
app.MapGet("roles", () => { })
    .WithTracking<RouteHandlerBuilder, DatabaseContext>(options =>
    {
        options.ProviderId = "my-sql-provider";
        options.Entities = [typeof(Role)];
    });
```

* **Direct Provider Instance**: Passes a concrete `ISourceProvider` implementation.

```cs
app.UseTracker<DatabaseContext>((options) =>
{
    options.SourceProvider = new SqlServerChangeTrackingOperations("my-sql-provider", "Data Source=localhost,1433;User ID=user;Password=password;Database=database;");
});

//minimal APIs
app.MapGet("roles", () => { })
    .WithTracking<RouteHandlerBuilder, DatabaseContext>(options =>
    {
        options.SourceProvider = 
            new SqlServerChangeTrackingOperations("my-sql-provider", "Data Source=localhost,1433;User ID=user;Password=password;Database=database;");
    });
```

* **Factory**: Uses a factory method for dynamic provider resolution.

```cs
app.UseTracker<DatabaseContext>((options) =>
{
    options.SourceProviderFactory = (httpContext) =>
    {
        return httpContext.RequestServices.GetKeyedService<ISourceProvider>("my-sql-provider") ??
            new SqlServerChangeTrackingOperations("my-sql-provider", "Data Source=localhost,1433;User ID=user;Password=password;Database=database;");
    };
});

//minimal APIs
app.MapGet("roles", () => { })
    .WithTracking<RouteHandlerBuilder, DatabaseContext>(options =>
    {
        options.SourceProviderFactory = (httpContext) =>
        {
            return httpContext.RequestServices.GetKeyedService<ISourceProvider>("my-sql-provider") ?? 
                new SqlServerChangeTrackingOperations("my-sql-provider", "Data Source=localhost,1433;User ID=user;Password=password;Database=database;");
        };
    });
```

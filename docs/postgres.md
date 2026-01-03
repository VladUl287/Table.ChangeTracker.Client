# PostgreSQL usage

Official documentation: [PostgreSQL Npgsql](https://www.npgsql.org).

This implementation uses PostgreSQL's built-in [track_commit_timestamp](https://www.postgresql.org/docs/17/runtime-config-replication.html#GUC-TRACK-COMMIT-TIMESTAMP) setting for global transaction tracking across the database, along with the custom [table_change_tracker](https://github.com/VladUl287/table_change_tracker) extension for monitoring modifications to specific tables.

## Implementation

PostgreSQL requires [track_commit_timestamp](https://www.postgresql.org/docs/17/runtime-config-replication.html#GUC-TRACK-COMMIT-TIMESTAMP) to be enabled for cases when no tables are specified in options.

This can be done using:

```sql
ALTER SYSTEM SET track_commit_timestamp = 'on';
```

Then restart the PostgreSQL service.

For cases when you need to track **specific tables**, use a custom extension developed specifically for that case: [table_change_tracker](https://github.com/VladUl287/table_change_tracker).

## Timestamp calculation

Global tracking:

```sql
SELECT pg_last_committed_xact();
```

Specific Table Tracking:

```sql
SELECT get_last_timestamp(@table_name);
SELECT get_last_timestamps(@tables_names);
```

When returning multiple timestamp ticks for multiple tables, they will be hashed with the [DefaultTrackerHasher](../Tracker.Core/Services/DefaultTrackerHasher.cs)

## Usage

```cs
var builder = WebApplication.CreateBuilder();
{
    builder.Services
        .AddTracker()
        .AddNpgsqlProvider<DatabaseContext>();
    
    builder.Services
        .AddTracker()
        .AddNpgsqlProvider<DatabaseContext>("my-pg-provider");

    builder.Services
        .AddTracker()
        .AddNpgsqlProvider(
            "my-pg-provider", 
            "Host=localhost;Port=5432;Database=mydb;Username=postgres;Password=secret"
        );
}
```

[snippet source](../Tracker.Npgsql/Extensions/ServiceCollectionExtensions.cs)

## Source Providers

All default source provider logic incapsulated in [NpgsqlOperations](../Tracker.Npgsql/Services/NpgsqlOperations.cs) class.
Providers resolbed by [DefaultProviderResolver](../Tracker.AspNet/Services//DefaultProviderResolver.cs)

All source providers must include an `Id` field, which is used to register them as keyed dependency injection services. For multi-database support, they must also implement the ISourceProvider interface. When using `.AddNpgsqlProvider<DatabaseContext>()` without a specified `Id`, the database context's full type name will serve as the source provider `Id`:

```cs
typeof(DatabaseContext).FullName
```

### TrackAttribute

Selection a source provider for change tracking:

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
[Track(providerId: "my-pg-provider", tables: ["roles"])]
[Track<DatabaseContext>(**: "my-pg-provider", entities: [typeof(Role)])]

//middlewares
app.UseTracker<DatabaseContext>((options) =>
{
    options.ProviderId = "my-pg-provider";
    options.Entities = [typeof(Role)];
});

//minimal APIs
app.MapGet("roles", () => { })
    .WithTracking<RouteHandlerBuilder, DatabaseContext>(options =>
    {
        options.ProviderId = "my-pg-provider";
        options.Entities = [typeof(Role)];
    });
```

* **Direct Provider Instance**: Passes a concrete `ISourceProvider` implementation.

```cs
app.UseTracker<DatabaseContext>((options) =>
{
    options.SourceProvider = new NpgsqlOperations("my-pg-provider", "Host=localhost;Port=5432;Database=main;Username=postgres;Password=postgres");
});

//minimal APIs
app.MapGet("roles", () => { })
    .WithTracking<RouteHandlerBuilder, DatabaseContext>(options =>
    {
        options.SourceProvider = 
            new NpgsqlOperations("my-pg-provider", "Host=localhost;Port=5432;Database=main;Username=postgres;Password=postgres");
    });
```

* **Factory**: Uses a factory method for dynamic provider resolution.

```cs
app.UseTracker<DatabaseContext>((options) =>
{
    options.SourceProviderFactory = (httpContext) =>
    {
        return httpContext.RequestServices.GetKeyedService<ISourceProvider>("my-pg-provider") ??
            new NpgsqlOperations("my-pg-provider", "Host=localhost;Port=5432;Database=main;Username=postgres;Password=postgres");
    };
});

//minimal APIs
app.MapGet("roles", () => { })
    .WithTracking<RouteHandlerBuilder, DatabaseContext>(options =>
    {
        options.SourceProviderFactory = (httpContext) =>
        {
            return httpContext.RequestServices.GetKeyedService<ISourceProvider>("my-pg-provider") ?? 
                new NpgsqlOperations("my-pg-provider", "Host=localhost;Port=5432;Database=main;Username=postgres;Password=postgres");
        };
    });
```

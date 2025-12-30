# Change Tracker

Change Tracker is inspired by [Delta Project](https://github.com/SimonCropp/Delta)

Change Tracker is a .NET library for efficient HTTP caching using database change tracking.
It implements **304 Not Modified** responses by generating ETags based on database timestamps,
reducing server load while ensuring clients always receive current data.

## 📋 Overview

Change Tracker monitors database changes and generates ETags that combine:

* Assembly write time (when your application was built)
* Database timestamp (last data modification time)
* Custom suffix (optional runtime context)

When a client requests data with a cached ETag, the server compares it with the current state.
If unchanged, it returns **304 Not Modified** - the client uses its cached copy.
If changed, fresh data is returned with a new ETag.

## 💡 Ideal Use Case

* Read-heavy applications where data changes less frequently than it's read
* APIs serving semi-static data that changes periodically
* Applications needing reduced server load without compromising data freshness

## 📚 Documentation

* [PostgreSQL Docs](/docs/postgres.md) when using [PostgreSQL Npgsql](https://www.npgsql.org)
* [SQL Server Docs](/docs/sqlserver.md) when using [SQL Server SqlClient](https://github.com/dotnet/SqlClient)

## 🛠️ How It Works

### 1. Format

ETags follow this format:

```cs
{AssemblyWriteTime}-{DbTimeStamp}-{Suffix}
```

#### ⏰ Assembly Write Time

The last modification time of your web application's assembly:

```cs
public sealed class AssemblyTimestampProvider(Assembly assembly) : IAssemblyTimestampProvider
{
    public DateTimeOffset GetWriteTime()
    {
        ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));

        if (!File.Exists(assembly.Location))
            throw new FileNotFoundException($"Assembly file not found at '{assembly.Location}'");

        return File.GetLastWriteTimeUtc(assembly.Location);
    }
}
```

[snippet source](/Tracker.Core/Services/AssemblyTimestampProvider.cs)

#### 🗄️ Database Timestamp

Tracks when data was last modified. Implementation varies by database:

* [SQL Server timestamp calculation](/docs/sqlserver.md#timestamp-calculation)
* [Postgres timestamp calculation](/docs/postgres.md#timestamp-calculation)

#### 🎯 Custom Suffix (Optional)

Dynamic string based on HTTP context for fine-grained cache control:

```cs
var builder = WebApplication.CreateBuilder(args);
{
  builder.Services
     .AddTracker(options =>
     {
         options.Suffix = (httpContext) => "Suffix";
     });
}

var app = builder.Build();
{
    app.UseTracker(options =>
    {
        options.Suffix = (httpContext) => "Suffix";
    });

    app.MapGet("route", () => { })
      .WithTracking(options =>
      {
          options.Suffix = (httpContext) => "Suffix";
      });
}
```

#### ⚙️ ETag Generation & Comparison

Efficient comparison avoids string allocation when data is unchanged:

```cs
public sealed class DefaultETagProvider(IAssemblyTimestampProvider assemblyTimestampProvider) : IETagProvider
{
    private readonly string _assemblyTimestamp = 
        assemblyTimestampProvider.GetWriteTime().Ticks.ToString();

    public bool Compare(string etag, ulong lastTimestamp, string suffix);
    public string Generate(ulong lastTimestamp, string suffix);
}
```

[snippet source](/Tracker.Core/Services/DefaultETagProvider.cs)

### 🚀 2. Regisration

#### Chanage Tracker Registration

```cs
builder.Services.AddTracker();

builder.Services.AddTracker(new GlobalOptions()
{
    CacheControl = "max-age=60, stale-while-revalidate=60, stale-if-error=86400",
    Filter = (httpContext) => true
});

builder.Services.AddTracker(options =>
{
    options.CacheControl = "max-age=60, stale-while-revalidate=60, stale-if-error=86400";
    options.Filter = (httpContext) => true;
});
```

#### Provider Documentation

For Change Tracker to function correctly, you must register a database-specific source provider.
This component monitors database changes and provides timestamps for ETag generation.

Detailed implementation guides for each database:

* [PostgreSQL Docs](/docs/postgres.md) when using [PostgreSQL Npgsql](https://www.npgsql.org)
* [SQL Server Docs](/docs/sqlserver.md) when using [SQL Server SqlClient](https://github.com/dotnet/SqlClient)

Available Provider Implementations

##### PostgreSQL (Npgsql)

```cs
// Register with DbContext
builder.Services
    .AddTracker()
    .AddNpgsqlProvider<DatabaseContext>();

// With custom provider identifier
builder.Services
    .AddTracker()
    .AddNpgsqlProvider<DatabaseContext>("my-pg-provider");

// Direct connection string registration
builder.Services
    .AddTracker()
    .AddNpgsqlProvider(
        "my-pg-provider", 
        "Host=localhost;Port=5432;Database=mydb;Username=postgres;Password=secret"
    );
```

[default npgsql provider](/Tracker.Npgsql/Services/NpgsqlOperations.cs)

##### SQL Server (SqlClient)

SQL Server supports multiple tracking modes for different scenarios:

```cs
// Default registration with DbContext
builder.Services
    .AddTracker()
    .AddSqlServerProvider<DatabaseContext>();

// Different tracking modes available:
builder.Services
    .AddTracker()
    .AddSqlServerProvider<DatabaseContext>(TrackingMode.DbIndexUsageStats)
    .AddSqlServerProvider<DatabaseContext>(TrackingMode.ChangeTracking);

// With custom provider ID
builder.Services
    .AddTracker()
    .AddSqlServerProvider<DatabaseContext>("my-sql-provider", TrackingMode.DbIndexUsageStats);

// Direct connection string registration
builder.Services
    .AddTracker()
    .AddSqlServerProvider(
        "my-sql-provider",
        "Server=localhost;Database=mydb;User Id=sa;Password=secret;",
        TrackingMode.ChangeTracking
    );
```

[default ChangeTrackingProvider](/Tracker.SqlServer/Services/SqlServerChangeTrackingOperations.cs) | [default DbIndexUsageStatsProvider](/Tracker.SqlServer/Services/SqlServerIndexUsageOperations.cs)

### 3. Usage

#### Controller Action (MVC/Web API)

Apply caching to specific endpoints using the [Track] attribute:

```cs
[HttpGet]
[Track(tables: ["roles"], cacheControl: "no-cache")]
public ActionResult<IEnumerable<Role>> GetAll() 
{
    return dbContext.Roles.ToList();
}
```

#### Middleware Configuration

Apply caching globally:

```cs
app.UseTracker(options =>
{
    options.CacheControl = "max-age=60, stale-while-revalidate=60, stale-if-error=86400";
    options.Filter = (httpContext) => 
        httpContext.Request.Path.Value!.Contains("/api/") &&
        httpContext.Request.Method == HttpMethods.Get;
});
```

#### Minimal APIs

Configure tracking directly on minimal API endpoints:

```cs
app.MapGet("/api/user-profile", () => 
{
    // Your endpoint logic
})
.WithTracking(options =>
{
    options.Tables = ["users", "profiles", "preferences"];
    options.CacheControl = "max-age=300"; // 5 minutes
});
```

## 🧪 Verifying behavior

### Testing Cache Hits

* Open your application in a browser
* Open Developer Tools (F12)
* Navigate to the Network tab
* Refresh the page

**Cached responses** will show:

* Status: 304 Not Modified
* Request Header: if-none-match (with ETag value)
* Response Header: etag (current ETag)

### Testing Cache Misses

To test the full request pipeline:

* Open Developer Tools → Network tab
* Check "Disable cache" in the toolbar
* Refresh the page

This prevents the browser from sending if-none-match, forcing a cache miss and full server execution.

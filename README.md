# ChangeTracker

[![NuGet Status](https://img.shields.io/nuget/v/ChangeTracker.Core.svg?label=ChangeTracker.Core)](https://www.nuget.org/packages/ChangeTracker.Core/)
[![NuGet Status](https://img.shields.io/nuget/v/ChangeTracker.AspNet.svg?label=ChangeTracker.AspNet)](https://www.nuget.org/packages/ChangeTracker.AspNet/)
[![NuGet Status](https://img.shields.io/nuget/v/ChangeTracker.Npgsql.svg?label=ChangeTracker.Npgsql)](https://www.nuget.org/packages/ChangeTracker.Npgsql/)
[![NuGet Status](https://img.shields.io/nuget/v/ChangeTracker.SqlServer.svg?label=ChangeTracker.SqlServer)](https://www.nuget.org/packages/ChangeTracker.SqlServer/)
[![NuGet Status](https://img.shields.io/nuget/v/ChangeTracker.FastEndpoints.svg?label=ChangeTracker.FastEndpoints)](https://www.nuget.org/packages/ChangeTracker.FastEndpoints/)

ChangeTracker is inspired by [Delta Project](https://github.com/SimonCropp/Delta)

Change Tracker is a library for efficient HTTP caching using database change tracking.
It implements [**304 Not Modified**](https://www.keycdn.com/support/304-not-modified) responses
by generating ETags based on database timestamps, reducing server load while ensuring
clients always receive current data.

## 📋 Overview

ChangeTracker monitors database changes and generates ETags that combine:

* Assembly write time (when your application was built)
* Database timestamp (last data modification time)
* Custom suffix (optional runtime context)

When a client requests data with a cached ETag, the server compares it with the current state.

* **If unchanged:** it returns **304 Not Modified** and the client uses its cached copy.
* **If changed:** fresh data is returned with a new ETag.

ETags follow this format:

```cs
{AssemblyWriteTime}-{DbTimeStamp}-{Suffix}
```

## 💡 Ideal Use Case

* Read-heavy applications where data changes less frequently than it's read
* APIs serving semi-static data that changes periodically
* Applications needing reduced server load without compromising data freshness

## 📚 Documentation

* [PostgreSQL](/docs/postgres.md) docs
* [SQL Server](/docs/sqlserver.md) docs

## 🛠️ How It Works

### Assembly Write Time

The last modification time of your web application's assembly is handled by the [AssemblyTimestampProvider](/Tracker.Core/Services/AssemblyTimestampProvider.cs), which implements the [IAssemblyTimestampProvider](/Tracker.Core/Services/Contracts/IAssemblyTimestampProvider.cs) interface.

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

### Database Timestamp

Tracks when data was last modified. Implementation varies by database:

* [PostgresSQL](/docs/postgres.md#timestamp-calculation) timestamp calculation
* [SQL Server](/docs/sqlserver.md#timestamp-calculation) timestamp calculation

### Custom Suffix (Optional)

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

### ETag Generation & Comparison

For comparison and generation of ETags, see the implementation in [DefaultETagProvider](/Tracker.Core/Services/DefaultETagProvider.cs) of the [IETagProvider](/Tracker.Core/Services/Contracts/IETagProvider.cs) interface.

### Chanage Tracker Client Registration

Tracker services can be registered using the [AddTracker](/Tracker.AspNet/Extensions/ServiceCollectionExtensions.cs) extension method, which accepts a [GlobalOptions](/Tracker.AspNet/Models/GlobalOptions.cs) configuration object.

```cs
builder.Services.AddTracker();

builder.Services.AddTracker(new GlobalOptions()
{
    CacheControl = "max-age=60, stale-while-revalidate=60, stale-if-error=86400",
});

builder.Services.AddTracker(options =>
{
    options.Filter = (httpContext) => true;
});
```

### Provider Documentation

For ChangeTracker to **function correctly**, you must register a database-specific source provider.
This component monitors database changes and provides timestamps for ETag generation.

Detailed implementation guides for each database:

* [PostgreSQL](/docs/postgres.md) docs
* [SQL Server](/docs/sqlserver.md) docs

## 🔧 Usage

### Controller Action (MVC/Web API)

Apply caching to specific endpoints using the [Track] attribute:

```cs
[HttpGet]
[Track(tables: ["roles"], cacheControl: "no-cache")]
public ActionResult<IEnumerable<Role>> GetAll() 
{
    return dbContext.Roles.ToList();
}
```

### Middleware Configuration

Apply caching globally:

```cs
app.UseTracker(options =>
{
    options.CacheControl = "max-age=60, stale-while-revalidate=60, stale-if-error=86400";
    options.Filter = (httpContext) => httpContext.Request.Path.Value.Contains("/api/");
});
```

### Minimal APIs

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

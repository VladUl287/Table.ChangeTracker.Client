# PostgreSQL usage

Official documentation: [PostgreSQL Npgsql](https://www.npgsql.org).

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

When returning multiple timestamp ticks for multiple tables, they will be hashed with the default hasher:

```cs
public sealed class DefaultTrackerHasher : ITrackerHasher
{
    public ulong Hash(ReadOnlySpan<long> versions)
    {
        if (BitConverter.IsLittleEndian)
            return XxHash3.HashToUInt64(MemoryMarshal.AsBytes(versions));

        return HashBigEndian(versions);
    }
}
```

[snippet source](../Tracker.Core/Services/DefaultTrackerHasher.cs)

## Usage

### Add to WebApplicationBuilder

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

### Add to a Route Group

To add to a specific [Route Group](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/route-handlers#route-groups):

```cs
app.MapGroup("/group")
    .WithTracking()
    .MapGet("/", () => { });
```

### Source Providers

Any provider implements:

```cs
public interface ISourceProvider : IDisposable
{
    string Id { get; }

    ValueTask<long> GetLastVersion(string key, CancellationToken token = default);

    ValueTask GetLastVersions(ImmutableArray<string> keys, long[] versions, CancellationToken token = default);

    ValueTask<long> GetLastVersion(CancellationToken token = default);

    ValueTask<bool> EnableTracking(string key, CancellationToken token = default);

    ValueTask<bool> DisableTracking(string key, CancellationToken token = default);

    ValueTask<bool> IsTracking(string key, CancellationToken token = default);

    ValueTask<bool> SetLastVersion(string key, long value, CancellationToken token = default);
}
```

[snippet source](../Tracker.Core/Services/Contracts/ISourceProvider.cs)

Source Provider Id is used for cases when multiple providers are registered. In cases with DbContext, the full name will be used as Source Provider Id:

```cs
typeof(DatabaseContext).FullName
```

[default npgsql provider implementation](../Tracker.Npgsql/Services/NpgsqlOperations.cs)

- For resolving providers, the default implementation of IProviderResolver is called. It resolves by provider id from keyed services if the providerId is present in options.
- If ProviderId is not present in options, it will check if a SourceProvider property instance is present.
- If SourceProvider is not present, it will try to check if a factory is present and call it if it is. If none of these are present, it will resolve the first ISourceProvider from the DI container.

[Implementation snippet](../Tracker.AspNet/Services//DefaultProviderResolver.cs)

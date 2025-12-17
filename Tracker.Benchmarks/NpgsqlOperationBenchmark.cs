using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Tracker.Npgsql.Services;

namespace Tracker.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, iterationCount: 10, warmupCount: 5)]
public class NpgsqlOperationBenchmark
{
    private readonly NpgsqlOperations _npgsqlOperations = new("1", "Host=localhost;Port=5432;Database=main;Username=postgres;Password=postgres");
    private const string _tableName = "roles";

    [Benchmark]
    public ValueTask<DateTimeOffset> GetRolesTimestamp()
    {
        return _npgsqlOperations.GetLastTimestamp(_tableName, default);
    }
}

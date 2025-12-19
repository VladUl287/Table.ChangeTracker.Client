using Npgsql;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tracker.Npgsql.Tests")]

namespace Tracker.Npgsql.Extensions;

internal static class ReaderExtensions
{
    private const long PostgresTimestampOffsetTicks = 630822816000000000L;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetTimestampTicks(this NpgsqlDataReader reader, int ordinal) =>
        reader.GetInt64(ordinal) * 10 + PostgresTimestampOffsetTicks;
}

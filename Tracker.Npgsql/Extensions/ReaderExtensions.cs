using System.Data.Common;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tracker.Npgsql.Tests")]

namespace Tracker.Npgsql.Extensions;

internal static class ReaderExtensions
{
    private const long PostgresTimestampOffsetTicks = 630822816000000000L;

    /// <summary>
    /// Adapted from <a href="https://github.com/npgsql/npgsql/blob/main/src/Npgsql/Internal/Converters/Temporal/PgTimestamp.cs">source documentation</a>
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="ordinal"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetTimestampTicks(this DbDataReader reader, int ordinal) =>
        reader.GetInt64(ordinal) * 10 + PostgresTimestampOffsetTicks;
}

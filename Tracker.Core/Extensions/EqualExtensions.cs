namespace Tracker.Core.Extensions;

public static class EqualExtensions
{
    public static bool EqualsLong(this ReadOnlySpan<char> chars, ulong number)
    {
        if (chars.Length > 19)
            return false;

        ulong result = 0;
        foreach (var c in chars)
        {
            if (c < '0' || c > '9') return false;
            result = result * 10 + (ulong)(c - '0');
        }

        return result == number;
    }
}

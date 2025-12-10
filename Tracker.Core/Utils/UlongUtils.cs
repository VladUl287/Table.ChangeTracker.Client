using System.Runtime.CompilerServices;

namespace Tracker.Core.Utils;

public static class UlongUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DigitCount(ulong n)
    {
        if (n == 0) return 1;
        return (int)Math.Floor(Math.Log10(n)) + 1;
    }
}

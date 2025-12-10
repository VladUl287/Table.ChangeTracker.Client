using System.Reflection;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Extensions;

namespace Tracker.AspNet.Services;

public class ETagService(Assembly executionAssembly) : IETagService
{
    private readonly string _assemblyBuildTime = executionAssembly.GetAssemblyWriteTime().Ticks.ToString();

    public bool EqualsTo(string ifNoneMatch, ulong lastTimestamp, string suffix)
    {
        var fullLength = ComputeLength(lastTimestamp, suffix);
        if (fullLength != ifNoneMatch.Length)
            return false;

        var ltDigitCount = lastTimestamp.CountDigits();
        var incomingETag = ifNoneMatch.AsSpan();
        var rightEdge = _assemblyBuildTime.Length;
        var inAsBuildTime = incomingETag[..rightEdge];
        if (!inAsBuildTime.Equals(_assemblyBuildTime.AsSpan(), StringComparison.Ordinal))
            return false;

        var inTicks = incomingETag.Slice(++rightEdge, ltDigitCount);
        if (!inTicks.EqualsLong(lastTimestamp))
            return false;

        rightEdge += ltDigitCount;
        if (rightEdge == incomingETag.Length)
            return true;

        var inSuffix = incomingETag[++rightEdge..];
        if (!inSuffix.Equals(suffix, StringComparison.Ordinal))
            return false;

        return true;
    }

    public string Build(ulong lastTimestamp, string suffix)
    {
        var fullLength = ComputeLength(lastTimestamp, suffix);
        return string.Create(fullLength, (_assemblyBuildTime, lastTimestamp, suffix), (chars, state) =>
        {
            var (asBuildTime, lastTimestamp, suffix) = state;

            var position = asBuildTime.Length;
            asBuildTime.AsSpan().CopyTo(chars);
            chars[position++] = '-';

            lastTimestamp.TryFormat(chars[position..], out var written);

            if (!string.IsNullOrEmpty(suffix))
            {
                position += written;
                chars[position++] = '-';
                suffix.AsSpan().CopyTo(chars[position..]);
            }
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ComputeLength(ulong lastTimestamp, string suffix) =>
        _assemblyBuildTime.Length + lastTimestamp.CountDigits() + suffix.Length + (suffix.Length > 0 ? 2 : 1);

}

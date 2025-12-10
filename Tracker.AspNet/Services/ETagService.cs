using System.Reflection;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Extensions;
using Tracker.Core.Utils;

namespace Tracker.AspNet.Services;

public class ETagService(Assembly executionAssembly) : IETagService
{
    private readonly string _assemblyBuildTimeTicks = executionAssembly.GetAssemblyWriteTime().Ticks.ToString();

    public string AssemblyBuildTimeTicks => _assemblyBuildTimeTicks;

    public bool EqualsTo(int fullLength, string srcETag, ulong lastTimestamp, string suffix)
    {
        if (fullLength != srcETag.Length)
            return false;

        var ltDigitCount = UlongUtils.DigitCount(lastTimestamp);
        var incomingETag = srcETag.AsSpan();
        var rightEdge = _assemblyBuildTimeTicks.Length;
        var inAsBuildTime = incomingETag[..rightEdge];
        if (!inAsBuildTime.Equals(_assemblyBuildTimeTicks.AsSpan(), StringComparison.Ordinal))
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

    public string Build(int fullLength, ulong lastTimestamp, string suffix)
    {
        return string.Create(fullLength, (_assemblyBuildTimeTicks, lastTimestamp, suffix), (chars, state) =>
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
}

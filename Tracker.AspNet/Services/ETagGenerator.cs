using System.Reflection;
using Tracker.Core.Extensions;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

public class ETagGenerator(Assembly executionAssembly) : IETagGenerator
{
    private readonly DateTimeOffset AssemblyBuildTime = executionAssembly.GetAssemblyWriteTime();

    public string GenerateETag(DateTimeOffset timestamp, string suffix)
    {
        var etag = $"{AssemblyBuildTime.Ticks}-{timestamp.Ticks}";
        if (!string.IsNullOrEmpty(suffix))
            etag += $"-{suffix}";
        return etag;
    }

    public string GenerateETag(DateTimeOffset[] timestamps, string suffix)
    {
        long xorResult = 0;

        foreach (var timestamp in timestamps)
            xorResult ^= timestamp.UtcTicks;

        var x16 = xorResult.ToString("x16");
        var etag = $"{AssemblyBuildTime.Ticks}-{x16}";
        if (!string.IsNullOrEmpty(suffix))
            etag += $"-{suffix}";
        return etag;
    }

    public string GenerateETag(uint xact, string suffix)
    {
        var x16 = xact.ToString("x16");
        var etag = $"{AssemblyBuildTime.Ticks}-{x16}";
        if (!string.IsNullOrEmpty(suffix))
            etag += $"-{suffix}";
        return etag;
    }
}

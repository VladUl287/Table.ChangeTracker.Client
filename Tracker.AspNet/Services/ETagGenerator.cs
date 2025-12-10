using System.Reflection;
using Tracker.Core.Extensions;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

public class ETagGenerator(Assembly executionAssembly) : IETagGenerator
{
    private readonly string _assemblyBuildTimeTicks = executionAssembly.GetAssemblyWriteTime().Ticks.ToString();

    public string AssemblyBuildTimeTicks => _assemblyBuildTimeTicks;

    public string BuildETag(int fullLength, ulong lastTimestamp, string suffix)
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

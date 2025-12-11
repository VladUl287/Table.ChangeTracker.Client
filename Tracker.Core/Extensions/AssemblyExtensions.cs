using System.Reflection;

namespace Tracker.Core.Extensions;

public static class AssemblyExtensions
{
    public static DateTimeOffset GetAssemblyWriteTime(this Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));
        ArgumentException.ThrowIfNullOrEmpty(assembly.Location, nameof(assembly.Location));

        if (!File.Exists(assembly.Location))
            throw new FileNotFoundException(
                $"Cannot determine write time for assembly '{assembly.FullName}'. " +
                $"Assembly file not found at '{assembly.Location}'");

        var lastWriteTimeUtc = File.GetLastWriteTimeUtc(assembly.Location);
        return new DateTimeOffset(lastWriteTimeUtc);
    }
}

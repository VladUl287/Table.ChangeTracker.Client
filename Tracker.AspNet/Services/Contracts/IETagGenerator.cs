namespace Tracker.AspNet.Services.Contracts;

public interface IETagGenerator
{
    string AssemblyBuildTimeTicks { get; }
    string BuildETag(int fullLength, ulong lastTimestamp, string suffix);
}

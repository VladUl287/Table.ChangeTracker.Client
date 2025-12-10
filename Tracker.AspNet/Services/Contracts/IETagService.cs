namespace Tracker.AspNet.Services.Contracts;

public interface IETagService
{
    string AssemblyBuildTimeTicks { get; }
    bool EqualsTo(int fullLength, string srcETag, ulong lastTimestamp, string suffix);
    string Build(int fullLength, ulong lastTimestamp, string suffix);
}

namespace Tracker.AspNet.Services.Contracts;

public interface IETagService
{
    bool EqualsTo(string ifNoneMatch, ulong lastTimestamp, string suffix);
    string Build(ulong lastTimestamp, string suffix);
}

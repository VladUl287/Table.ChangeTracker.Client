namespace Tracker.AspNet.Services.Contracts;

public interface IETagGenerator
{
    string GenerateETag(DateTimeOffset timestamp, string suffix);
    string GenerateETag(DateTimeOffset[] timestamps, string suffix);
    string GenerateETag(uint xact, string suffix);
}

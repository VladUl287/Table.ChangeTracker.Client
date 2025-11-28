namespace Npgsql.EFCore.Tracker.AspNet.Services.Contracts;

public interface IETagGenerator
{
    string GenerateETag(DateTimeOffset timestamp);
    string GenerateETag(params DateTimeOffset[] timestamps);
}

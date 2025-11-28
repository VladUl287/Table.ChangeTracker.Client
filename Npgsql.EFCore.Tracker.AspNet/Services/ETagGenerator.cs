using Npgsql.EFCore.Tracker.AspNet.Services.Contracts;
using Npgsql.EFCore.Tracker.AspNet.Utils;

namespace Npgsql.EFCore.Tracker.AspNet.Services;

public class ETagGenerator : IETagGenerator
{
    public string GenerateETag(DateTimeOffset timestamp)
    {
        return ETagUtils.GenETagTicks(timestamp);
    }

    public string GenerateETag(params DateTimeOffset[] timestamps)
    {
        return ETagUtils.GenETagTicks(timestamps);
    }
}

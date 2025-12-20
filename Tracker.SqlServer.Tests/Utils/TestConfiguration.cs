namespace Tracker.SqlServer.Tests.Utils;

public static class TestConfiguration
{
    public static string GetConnectionString()
    {
        return "Data Source=localhost,1433;User ID=sa;Password=Password1;Database=TrackerTestDb;TrustServerCertificate=True;";
    }

    public static string GetLowPrivilageConnectionString()
    {
        return "Data Source=localhost,1433;User ID=lowprivilege;Password=Password1;Database=TrackerTestDb;TrustServerCertificate=True;";
    }
}


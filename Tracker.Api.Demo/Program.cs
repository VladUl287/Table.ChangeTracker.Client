using Microsoft.EntityFrameworkCore;
using Tracker.Api.Demo.Database;
using Tracker.AspNet.Extensions;
using Tracker.Npgsql.Extensions;
using Tracker.SqlServer.Extensions;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddControllers();

    builder.Services.AddOpenApi();

    builder.Services
        .AddTracker()
        .AddNpgsqlProvider<DatabaseContext>()
        .AddSqlServerProvider<SqlServerDatabaseContext>()
        .AddNpgsqlProvider("source1", "Host=localhost;Port=5432;Database=test123;Username=postgres;Password=postgres")
        .AddSqlServerProvider("source2", "Server=localhost;Database=TrackerTestDb;Trusted_Connection=true;")
        ;

    builder.Services.AddDbContext<DatabaseContext>(options =>
    {
        options
            .UseNpgsql("Host=localhost;Port=5432;Database=main;Username=postgres;Password=postgres")
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
    });

    builder.Services.AddDbContext<SqlServerDatabaseContext>(options =>
    {
        options
            .UseSqlServer("Data Source=localhost,1433;User ID=sa;Password=Password1;Database=TrackerTestDb;TrustServerCertificate=True;")
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
    });
}

var app = builder.Build();
{
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseCors(config => config
        .AllowAnyMethod()
        .AllowAnyMethod()
        .AllowAnyOrigin());

    app.UseAuthorization();

    //app.UseTracker<DatabaseContext>(opt =>
    //{
    //    opt.Tables = ["roles"];
    //    opt.Entities = [typeof(Role)];
    //    opt.Filter = (ctx) => ctx.Request.Path.ToString().Contains("roles");
    //});

    //app.MapGet("/api/role", () => "Get all roles")
    //    .WithTracking();

    //app.MapGet("/api/v2/role", () => "Get all roles")
    //    .WithTracking<DatabaseContext>((opt) =>
    //    {
    //        opt.Tables = ["roles"];
    //    });

    //app.MapGet("/api/role/table", () => "Get all roles with table")
    //    .WithTracking<DatabaseContext>((opt) =>
    //    {
    //        opt.Entities = [typeof(Role)];
    //    });

    app.MapControllers();
}
app.Run();

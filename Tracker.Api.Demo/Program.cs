using Microsoft.EntityFrameworkCore;
using Tracker.Api.Demo.Database;
using Tracker.AspNet.Extensions;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddControllers();

    builder.Services.AddOpenApi();

    builder.Services
        .AddTracker<DatabaseContext>(conf =>
        {
        })
        .AddNpgsqlSource<DatabaseContext>()
        .AddNpgsqlSource("source1", "Host=localhost;Port=5432;Database=test123;Username=postgres;Password=postgres")
        .AddSqlServerSource("source2", "Server=localhost;Database=MyDb;Trusted_Connection=true;")
        .AddDbContext<DatabaseContext>(options =>
        {
            options
                .UseNpgsql("Host=localhost;Port=5432;Database=main;Username=postgres;Password=postgres")
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

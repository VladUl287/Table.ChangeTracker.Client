using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Tracker.Api.Demo.Database;
using Tracker.Api.Demo.Database.Entities;
using Tracker.AspNet.Extensions;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddControllers();

    builder.Services.AddOpenApi();

    builder.Services
        .AddTracker<DatabaseContext>(Assembly.GetExecutingAssembly())
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

    //app.UseTracker<DatabaseContext>();
    //app.UseTracker<DatabaseContext>(["roles"]);
    //app.UseTracker<DatabaseContext>([typeof(Role)]);

    app.MapGet("/api/role", () => "Get all roles")
        .WithTracking(tables: ["roles"]);

    app.MapGet("/api/role/table", () => "Get all roles with table")
        .WithTracking<DatabaseContext>(entities: [typeof(Role)]);

    app.MapControllers();
}
app.Run();

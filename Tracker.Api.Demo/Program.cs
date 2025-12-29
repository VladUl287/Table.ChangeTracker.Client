using Microsoft.EntityFrameworkCore;
using Tracker.Api.Demo.Database;
using Tracker.AspNet.Extensions;
using Tracker.Npgsql.Extensions;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddControllers();

    builder.Services.AddOpenApi();

    builder.Services
        .AddTracker()
        .AddNpgsqlProvider<DatabaseContext>();

    builder.Services.AddDbContext<DatabaseContext>(options =>
    {
        options
            .UseNpgsql("Host=localhost;Port=5432;Database=main;Username=postgres;Password=postgres")
            //.UseSqlServer("Data Source=localhost,1433;User ID=sa;Password=Password1;Database=TrackerTestDb;TrustServerCertificate=True;")
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

    app.MapControllers();
}
app.Run();

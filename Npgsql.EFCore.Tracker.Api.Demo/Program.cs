using Npgsql.EFCore.Tracker.AspNet.Extensions;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddControllers();

    builder.Services.AddOpenApi();

    builder.Services.AddTrackerSupport(Assembly.GetExecutingAssembly());
}

var app = builder.Build();
{
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();
}
app.Run();

using CloudWeather.Precipitation.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PrecipitationDbContext>(options =>
{
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
    options.UseNpgsql(builder.Configuration.GetConnectionString("AppDb"));
}, ServiceLifetime.Transient
);

var app = builder.Build();

app.MapGet("/observation/{zip}", async (string zip, [FromQuery] int? days, PrecipitationDbContext db) =>
{
    if (days == null || days < 1 || days > 30)
    {
        return Results.BadRequest("Provide days query between 1 and 30");
    }
    var startDate = DateTime.UtcNow - TimeSpan.FromDays(days.Value);
    var results = await db.Precipitation
    .Where(x => x.ZipCode == zip && x.CreatedOn > startDate)
    .ToListAsync();

    return Results.Ok(results);
});

app.MapPost("/observation/", async (Precipitation precipitation, PrecipitationDbContext db) =>
{
    precipitation.CreatedOn = precipitation.CreatedOn.ToUniversalTime();
    await db.AddAsync(precipitation);
    await db.SaveChangesAsync();
});

app.Run();
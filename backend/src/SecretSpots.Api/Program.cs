using System.Reflection;
using FluentValidation;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;

var builder = WebApplication.CreateBuilder(args);

var featuresAssembly = Assembly.Load("SecretSpots.Features");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Postgres' configuration.");
builder.Services.AddPersistence(connectionString);

builder.Services.AddMediator(featuresAssembly);
builder.Services.AddValidatorsFromAssembly(featuresAssembly);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        // Real origins (web/mobile) get added here once those clients exist.
        policy.WithOrigins();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Default");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("HealthCheck");

app.Run();

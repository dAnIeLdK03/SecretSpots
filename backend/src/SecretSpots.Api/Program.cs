using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SecretSpots.Api;
using SecretSpots.Features.Auth;
using SecretSpots.Features.CheckIns;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.ExceptionHandling;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Common.Storage;
using SecretSpots.Features.Photos;
using SecretSpots.Features.Spots;

var builder = WebApplication.CreateBuilder(args);

var featuresAssembly = Assembly.Load("SecretSpots.Features");

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT access token.",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
});

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(StartupMessages.MissingPostgresConnectionString);
builder.Services.AddPersistence(connectionString);

builder.Services.AddMediator(featuresAssembly);
builder.Services.AddValidatorsFromAssembly(featuresAssembly);

builder.Services.AddLocalization();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<CrystalsOptions>(builder.Configuration.GetSection("Crystals"));
builder.Services.Configure<CheckInOptions>(builder.Configuration.GetSection("CheckIn"));
builder.Services.Configure<R2Options>(builder.Configuration.GetSection("R2"));
builder.Services.Configure<PhotoOptions>(builder.Configuration.GetSection("Photos"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<IPhotoStorage, R2PhotoStorage>();

// Kestrel's own default MaxRequestBodySize (~28.6MB) is looser than our actual photo size
// limit — without this, an oversized-but-under-Kestrel's-cap upload would be fully received
// and buffered before UploadPhoto's FluentValidation check gets a chance to reject it.
var maxPhotoFileSizeBytes = builder.Configuration.GetValue<long?>("Photos:MaxFileSizeBytes")
    ?? new PhotoOptions().MaxFileSizeBytes;
const long multipartOverheadBytes = 1024 * 1024;
builder.WebHost.ConfigureKestrel(serverOptions =>
    serverOptions.Limits.MaxRequestBodySize = maxPhotoFileSizeBytes + multipartOverheadBytes);

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
if (string.IsNullOrWhiteSpace(jwtOptions?.Secret))
{
    throw new InvalidOperationException(StartupMessages.MissingJwtConfiguration);
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Without this, JwtSecurityTokenHandler remaps "sub" to the legacy
        // ClaimTypes.NameIdentifier URI, and IUserContext (which reads "sub" literally) would
        // never find it on an otherwise validly authenticated request.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

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

// Registered early so it wraps every middleware/endpoint that follows.
app.UseValidationExceptionHandling();

app.UseHttpsRedirection();

string[] supportedCultures = ["bg", "en"];
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("bg")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("HealthCheck");

app.MapAuthEndpoints();
app.MapSpotsEndpoints();
app.MapCheckInsEndpoints();
app.MapPhotosEndpoints();

app.Run();

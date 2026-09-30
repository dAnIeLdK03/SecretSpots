using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SecretSpots.Api;
using SecretSpots.Features.Auth;
using SecretSpots.Features.Businesses;
using SecretSpots.Features.CheckIns;
using SecretSpots.Features.Comments;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Email;
using SecretSpots.Features.Common.ExceptionHandling;
using SecretSpots.Features.Common.ExternalAuth;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Observability;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Common.Storage;
using SecretSpots.Features.Notifications;
using SecretSpots.Features.Photos;
using SecretSpots.Features.Ratings;
using SecretSpots.Features.Reports;
using SecretSpots.Features.Rewards;
using SecretSpots.Features.SavedSpots;
using SecretSpots.Features.Spots;
using WebPush;

var builder = WebApplication.CreateBuilder(args);

// No-ops when Sentry:Dsn is unset (e.g. local dev), so this is safe to leave wired up everywhere.
// Registered before everything else so it can also capture startup failures.
builder.WebHost.UseSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:Dsn"];
    options.Environment = builder.Environment.EnvironmentName;
    options.SendDefaultPii = false;
});

// Structured console logs, both formats including logging scopes so the correlation id pushed by
// UseCorrelationId (see below) actually shows up on every log line written for a request — not
// just the one line that started it. JSON in Production because that's what an actual log
// aggregator (Render/Koyeb capture stdout) can parse into a queryable field instead of a blob of
// text; plain readable text in Development, where a human is tailing the console directly.
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddSimpleConsole(options => options.IncludeScopes = true);
}
else
{
    builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
}

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

// Readiness check (see /health/ready below) — actually opens a connection and runs a trivial
// query against Postgres, unlike /health which only proves the process is alive and accepting
// requests. A DB outage/misconfiguration then shows up as "not ready" instead of a plain 200 that
// hides the real problem until the first real request fails.
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

builder.Services.Configure<TokenCleanupOptions>(builder.Configuration.GetSection("TokenCleanup"));
builder.Services.AddHostedService<TokenCleanupService>();

builder.Services.AddMediator(featuresAssembly);
builder.Services.AddValidatorsFromAssembly(featuresAssembly);

builder.Services.AddLocalization();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<CrystalsOptions>(builder.Configuration.GetSection("Crystals"));
builder.Services.Configure<CheckInOptions>(builder.Configuration.GetSection("CheckIn"));
builder.Services.Configure<CommentOptions>(builder.Configuration.GetSection("Comments"));
builder.Services.Configure<SpotSearchOptions>(builder.Configuration.GetSection("SpotSearch"));
builder.Services.Configure<ReportsOptions>(builder.Configuration.GetSection("Reports"));
builder.Services.Configure<R2Options>(builder.Configuration.GetSection("R2"));
builder.Services.Configure<PhotoOptions>(builder.Configuration.GetSection("Photos"));
builder.Services.Configure<NotificationsOptions>(builder.Configuration.GetSection("Notifications"));
builder.Services.Configure<SavedSpotsOptions>(builder.Configuration.GetSection("SavedSpots"));
builder.Services.Configure<RewardsOptions>(builder.Configuration.GetSection("Rewards"));
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("RateLimiting"));

builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection("GoogleAuth"));
builder.Services.Configure<FacebookAuthOptions>(builder.Configuration.GetSection("FacebookAuth"));
builder.Services.Configure<ExternalAuthOptions>(builder.Configuration.GetSection("ExternalAuth"));
builder.Services.Configure<ResendOptions>(builder.Configuration.GetSection("Resend"));
builder.Services.Configure<PasswordResetOptions>(builder.Configuration.GetSection("PasswordReset"));
builder.Services.Configure<EmailVerificationOptions>(builder.Configuration.GetSection("EmailVerification"));
builder.Services.Configure<WebPushOptions>(builder.Configuration.GetSection("WebPush"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<IPhotoStorage, R2PhotoStorage>();

builder.Services.AddHttpClient<GoogleAuthProvider>();
builder.Services.AddHttpClient<FacebookAuthProvider>();
builder.Services.AddScoped<IExternalAuthProvider>(sp => sp.GetRequiredService<GoogleAuthProvider>());
builder.Services.AddScoped<IExternalAuthProvider>(sp => sp.GetRequiredService<FacebookAuthProvider>());

builder.Services.AddHttpClient<WebPushClient>();

// In Development (local dev machines and the CI e2e job), no Resend API key is configured
// anywhere, so forgot-password would otherwise fail outright. Capturing sent emails in memory
// instead also lets e2e tests read back a password-reset link via /internal/test/emails below.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<ITestEmailInbox, TestEmailInbox>();
    builder.Services.AddSingleton<IEmailSender, InMemoryEmailSender>();
}
else
{
    builder.Services.AddHttpClient<IEmailSender, ResendEmailSender>();
}

// Kestrel's own default MaxRequestBodySize (~28.6MB) is looser than our actual photo size
// limit — without this, an oversized-but-under-Kestrel's-cap upload would be fully received
// and buffered before UploadPhoto's FluentValidation check gets a chance to reject it.
var maxPhotoFileSizeBytes = builder.Configuration.GetValue<long?>("Photos:MaxFileSizeBytes")
    ?? new PhotoOptions().MaxFileSizeBytes;
const long multipartOverheadBytes = 1024 * 1024;
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
    serverOptions.Limits.MaxRequestBodySize = maxPhotoFileSizeBytes + multipartOverheadBytes;
});

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
if (string.IsNullOrWhiteSpace(jwtOptions?.Secret))
{
    throw new InvalidOperationException(StartupMessages.MissingJwtConfiguration);
}

// HMAC-SHA256 (see JwtService) needs at least as many bits of key material as its output —
// 256 bits / 32 bytes (RFC 2104) — or the signature becomes brute-forceable well before the
// algorithm's own strength is the limiting factor.
const int minJwtSecretBytes = 32;
if (Encoding.UTF8.GetByteCount(jwtOptions.Secret) < minJwtSecretBytes)
{
    throw new InvalidOperationException(StartupMessages.WeakJwtSecret);
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireClaim(ClaimNames.IsAdmin, "true"));
});

// Partitions by authenticated user id rather than IP whenever one is available, so (a) an
// attacker rotating IPs can't just dodge the limit on write endpoints that require auth, and (b)
// legitimate users sharing one IP (an office, or mobile carrier NAT — common in BG) don't share
// one bucket. Requires UseAuthentication to run before UseRateLimiter in the pipeline below, or
// HttpContext.User is never populated at this point and every request falls back to IP anyway.
// Falls back to IP for anonymous requests (e.g. login/register), where there's no user id yet.
static string RateLimitPartitionKey(HttpContext context)
{
    var subClaim = context.User.Identity?.IsAuthenticated == true
        ? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        : null;

    return subClaim is not null
        ? $"user:{subClaim}"
        : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}

var rateLimitingOptions = builder.Configuration.GetSection("RateLimiting").Get<RateLimitingOptions>()
    ?? new RateLimitingOptions();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Every policy below uses a fixed-window limiter, which populates this metadata with how
    // long until the window resets — surface it as a real Retry-After header instead of leaving
    // the caller (including our own frontend) to guess when it's safe to retry.
    options.OnRejected = (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers["Retry-After"] =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        return ValueTask.CompletedTask;
    };

    // Broad per-IP safety net applied to every endpoint, on top of which the stricter named
    // policies below layer additional limits for specific endpoints.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimitPartitionKey(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitingOptions.GlobalPermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitingOptions.GlobalWindowSeconds),
            }));

    options.AddPolicy(RateLimitPolicies.Auth, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimitPartitionKey(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitingOptions.AuthPermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitingOptions.AuthWindowSeconds),
            }));

    options.AddPolicy(RateLimitPolicies.Photos, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimitPartitionKey(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitingOptions.PhotosPermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitingOptions.PhotosWindowSeconds),
            }));

    options.AddPolicy(RateLimitPolicies.ContentWrites, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimitPartitionKey(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitingOptions.ContentWritesPermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitingOptions.ContentWritesWindowSeconds),
            }));

    options.AddPolicy(RateLimitPolicies.Reports, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimitPartitionKey(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitingOptions.ReportsPermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitingOptions.ReportsWindowSeconds),
            }));
});

var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            // Retry-After isn't one of the handful of response headers a browser exposes to JS
            // by default on a cross-origin request — without this, the frontend's fetch() would
            // see the header arrive over the wire (visible in devtools) but get null from
            // response.headers.get('Retry-After'), making the header useless for an actual
            // retry/backoff UI.
            .WithExposedHeaders("Retry-After");
    });
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

var app = builder.Build();

// Must run before anything that reads the client IP or scheme — the rate limiter (partitioned
// by RemoteIpAddress), UseHttpsRedirection, and request logging all rely on this. Without it,
// every request behind a reverse proxy (Render/Koyeb in production always front the app with
// one) shows up with the proxy's IP, not the caller's — collapsing the per-IP rate limits
// (global, auth, photos) into one bucket shared by every user of the app.
//
// KnownNetworks/KnownProxies are cleared because PaaS platforms like Render/Koyeb don't expose
// a fixed, enumerable proxy IP to allowlist — their edge is the only way traffic reaches this
// app, so trusting X-Forwarded-For unconditionally is safe here. It would NOT be safe if this
// app were ever also directly internet-reachable, bypassing that edge — a client could spoof
// the header in that case.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Registered this early so the correlation id also covers the exception handler and every
// middleware below — an unhandled exception's log line still gets tagged with the same id that
// was echoed back to the caller.
app.UseCorrelationId();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Skipped in Development the same way the ASP.NET Core default template does — it sends
    // Strict-Transport-Security, which would otherwise make a browser refuse plain http://
    // localhost for the next year. In Production this closes the gap UseHttpsRedirection alone
    // leaves open: a redirect only takes effect on a request the server actually receives, so a
    // *first* plain-HTTP request (or one from a client that ignores the redirect) is still
    // interceptable. HSTS tells the browser to never attempt http:// for this host again after
    // the first successful https:// response.
    app.UseHsts();
}

// Registered early so it wraps every middleware/endpoint that follows.
app.UseValidationExceptionHandling();

app.UseHttpsRedirection();

// Baseline hardening headers. This is a JSON API with no HTML pages of its own outside Swagger
// (dev-only), so clickjacking/MIME-sniffing/referrer-leak risk here is already low — but every
// header below is a one-line no-downside addition, and defense-in-depth doesn't require the risk
// to be high to be worth closing.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
    if (!context.Request.Path.StartsWithSegments("/swagger"))
    {
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'";
    }

    await next();
});

string[] supportedCultures = ["bg", "en"];
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("bg")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

app.UseCors("Default");

// Authentication (not authorization — that still runs after the limiter, since it doesn't affect
// what bucket a request lands in) must run before UseRateLimiter so the partition key selectors
// below can read HttpContext.User: without this, every request — logged in or not — would look
// anonymous to the limiter and only ever fall back to the IP partition.
app.UseAuthentication();

app.UseRateLimiter();

app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("HealthCheck");

// Liveness (/health, above) only proves the process is up — it deliberately never touches the
// database, so a slow/unreachable DB can't make an otherwise-healthy process get killed by a
// liveness probe. This is readiness: does a dependency check (Postgres) actually succeed right
// now. A load balancer/orchestrator should route traffic based on this one, not /health.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()),
        });
    },
}).WithName("ReadinessCheck");

app.MapAuthEndpoints();
app.MapSpotsEndpoints();
app.MapCheckInsEndpoints();
app.MapCommentsEndpoints();
app.MapRatingsEndpoints();
app.MapSavedSpotsEndpoints();
app.MapPhotosEndpoints();
app.MapBusinessesEndpoints();
app.MapRewardsEndpoints();
app.MapNotificationsEndpoints();
app.MapReportsEndpoints();
app.MapAdminReportsEndpoints();

// Lets e2e tests read back what InMemoryEmailSender captured (see registration above) instead of
// needing a real inbox — e.g. to pull the token out of a password-reset link. Gated on
// IsDevelopment() the same way Swagger is above, so it can never exist in Production.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/internal/test/emails", (string to, ITestEmailInbox inbox) =>
    {
        var email = inbox.GetLatest(to);
        return email is null ? Results.NotFound() : Results.Ok(email);
    });

    // Lets e2e tests compute exactly how many requests trip the auth rate limit instead of
    // guessing/hardcoding a number that has to be kept in sync with appsettings or a CI-only
    // override.
    app.MapGet("/internal/test/rate-limit-config", (IOptions<RateLimitingOptions> options) =>
        Results.Ok(new { options.Value.AuthPermitLimit, options.Value.AuthWindowSeconds }));
}

app.Run();

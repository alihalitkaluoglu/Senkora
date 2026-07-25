using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Senkora.Api.Extensions;
using Senkora.Api.Hubs;
using Senkora.Api.Middleware;
using Senkora.Application;
using Senkora.Infrastructure;
using Senkora.Infrastructure.Persistence;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Senkora.Api")
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:Url"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// ── Application & Infrastructure ─────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── Controllers & Swagger ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(opts =>
{
    opts.MultipartBodyLengthLimit = 52_428_800; // 50 MB
});
builder.WebHost.ConfigureKestrel(opts =>
    opts.Limits.MaxRequestBodySize = 52_428_800);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfiguration();

// ── JWT ──────────────────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException(
        "Jwt:Secret is not configured. Check appsettings.Development.json.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer           = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"] ?? "https://api.senkora.io",
            ValidateAudience         = true,
            ValidAudience            = builder.Configuration["Jwt:Audience"] ?? "https://senkora.io",
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
        opts.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogError("JWT Auth failed: {Error}", ctx.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(opts =>
{
    // ClaimTypes.Role kullanildigi icin RequireRole kullanilmali
    opts.AddPolicy("SuperAdmin",  p => p.RequireRole("SuperAdmin"));
    opts.AddPolicy("TenantAdmin", p => p.RequireRole("TenantAdmin", "SuperAdmin"));
    opts.AddPolicy("SyncManager", p => p.RequireRole("SyncManager", "TenantAdmin", "SuperAdmin"));
});

// ── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p
        .WithOrigins(builder.Configuration
            .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(opts =>
    opts.AddFixedWindowLimiter("api", o =>
    {
        o.PermitLimit            = int.Parse(builder.Configuration["RateLimit:PermitLimit"] ?? "1000");
        o.Window                 = TimeSpan.FromSeconds(int.Parse(builder.Configuration["RateLimit:WindowSeconds"] ?? "60"));
        o.QueueProcessingOrder   = QueueProcessingOrder.OldestFirst;
        o.QueueLimit             = 50;
    }));

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── HttpContext ───────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "sqlserver", tags: ["db"])
    .AddRedis(
        builder.Configuration["Redis:ConnectionString"]!,
        name: "redis", tags: ["cache"]);

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Database Init (migrate + seed) ───────────────────────────────────────────
await DatabaseInitializer.InitializeAsync(app.Services);

// ── Middleware Pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Senkora API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseStaticFiles(); // wwwroot

// Yuklenen urun gorsellerini /uploads altinda sun
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(),
    builder.Configuration["FileStorage:LocalPath"] ?? "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath  = "/uploads"
});
app.UseAuthentication();
app.UseAuthorization();

// TenantMiddleware MUST come after UseAuthentication so context.User is populated
app.UseMiddleware<TenantMiddleware>();

app.MapControllers();
app.MapHangfireDashboard("/hangfire");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status  = report.Status.ToString(),
            checks  = report.Entries.Select(e => new
                      { name = e.Key, status = e.Value.Status.ToString() }),
            totalMs = report.TotalDuration.TotalMilliseconds
        }));
    }
});

app.MapHub<SyncHub>("/hubs/sync");

app.Run();

public partial class Program { }

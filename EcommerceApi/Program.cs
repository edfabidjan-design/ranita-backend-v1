using EcommerceApi.Data;
using EcommerceApi.Models;
using EcommerceApi.Service;
using EcommerceApi.Service.Background;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

QuestPDF.Settings.License = LicenseType.Community;

var projectRoot = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..")
);

var builderOptions = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.Exists(Path.Combine(projectRoot, "wwwroot"))
        ? projectRoot
        : Directory.GetCurrentDirectory(),
    WebRootPath = "wwwroot"
};

var builder = WebApplication.CreateBuilder(builderOptions);
builder.Services.AddControllers();

// =========================
// DB + Services
// =========================
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<PushService>();
builder.Services.AddScoped<CommissionResolver>();
builder.Services.AddScoped<OrderPaymentFinalizer>();
builder.Services.AddScoped<ReturnsService>();

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<QueuedHostedService>();

builder.Services.AddScoped<VendorContractPdfService>();

builder.Services.AddSignalR();

// ✅ Worker payout (48h)
// builder.Services.AddHostedService<VendorPayoutWorker>();

// =========================
// CORS
// =========================
builder.Services.AddCors(o =>
{
    o.AddPolicy("web", p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

builder.Services.AddEndpointsApiExplorer();

// =========================
// Auth JWT
// =========================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var j = builder.Configuration.GetSection("Jwt");
        var issuer = j["Issuer"] ?? "RanitaApi";
        var key = j["Key"] ?? throw new Exception("Jwt:Key manquant");

        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = issuer,

            // ✅ accepte ADMIN + CLIENT + VENDOR
            ValidAudiences = new[] { "RanitaAdmin", "RanitaClient", "RanitaVendor" },

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.FromMinutes(2),

            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/client-notifs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },

            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine("❌ JWT FAIL => " + ctx.Exception);
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                Console.WriteLine("⚠️ JWT CHALLENGE => " + ctx.Error + " | " + ctx.ErrorDescription);
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                var user = ctx.Principal;
                var aud = (ctx.SecurityToken as JwtSecurityToken)?.Audiences?.FirstOrDefault();
                var iss = (ctx.SecurityToken as JwtSecurityToken)?.Issuer;

                Console.WriteLine("✅ JWT OK => iss=" + iss + " aud=" + aud);
                Console.WriteLine("✅ ROLE => " + (user?.FindFirst(ClaimTypes.Role)?.Value ?? "(none)"));
                Console.WriteLine("✅ NAMEID => " + (user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "(none)"));
                return Task.CompletedTask;
            }
        };
    });

Console.WriteLine("CONN=" + builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddAuthorization();

// =========================
// Swagger
// =========================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EcommerceApi", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Entrez : Bearer {votre_token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// =========================
// Firebase (protégé)
// =========================
var firebasePath = builder.Configuration["Firebase:ServiceAccountPath"];
if (!string.IsNullOrWhiteSpace(firebasePath) && File.Exists(firebasePath))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebasePath)
    });
}
else
{
    Console.WriteLine("⚠️ Firebase ServiceAccount introuvable, Firebase désactivé (dev).");
}

builder.Services.AddHttpClient<WhatsAppService>();
builder.Services.AddScoped<EmailService>();

if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://0.0.0.0:7070");
}
// =========================
// Build
// =========================
var app = builder.Build();

// Debug connection
app.MapGet("/debug/connection", (AppDbContext db) =>
{
    return Results.Ok(new
    {
        ConnectionString = db.Database.GetConnectionString(),
        Database = db.Database.GetDbConnection().Database,
        DataSource = db.Database.GetDbConnection().DataSource
    });
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //await DbSeeder.SeedCommissionSettingsAsync(db);
}

// ✅ Bootstrap SuperAdmin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var superName = builder.Configuration["BootstrapAdmin:Username"] ?? "superadmin";
    var superPwd = builder.Configuration["BootstrapAdmin:Password"] ?? "admin1234";

    var exists = db.Users.Any(u => u.Username == superName);
    if (!exists)
    {
        db.Users.Add(new User
        {
            Username = superName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(superPwd),
            Role = "SuperAdmin",
            IsActive = true
        });

        db.SaveChanges();
        Console.WriteLine($"✅ SuperAdmin créé: {superName}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}




// wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();




var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});
// /uploads


app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<EcommerceApi.Hubs.ClientNotifHub>("/hubs/client-notifs");

// SEO fallbacks (TOUJOURS EN DERNIER)
app.MapFallbackToFile("/c/{*slug}", "products.html");
app.MapFallbackToFile("{*path:nonfile}", "home.html");



app.Run();
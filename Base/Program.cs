using Base.Middleware;
using System.Security.Claims;
using Base.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Threading.RateLimiting;
using Base.Common;
using Base.Configuration;
using Base.Data;
using Base.Entities;
using Base.Extensions;
using Base.Repositories;
using Base.Repositories.IRepositories;
using Base.Services;
using Base.Services.IServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMemoryCache();
builder.Services.AddScoped<Base.Gold.IGoldRepository, Base.Gold.GoldRepository>();
builder.Services.AddScoped<Base.Gold.GoldService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient<Base.Gold.IFaceVerifier, Base.Gold.FaceVerifier>(client => client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddHttpClient<Base.Gold.IFaceEnrollment, Base.Gold.FaceEnrollment>(client => client.Timeout = TimeSpan.FromSeconds(15));
var webRoot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRoot, "uploads"));
builder.Environment.WebRootPath = webRoot;
builder.Environment.WebRootFileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot);

builder.Services.AddScoped<AuditContext>();
builder.Services.AddApiPresentation();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
builder.Services.AddScoped<ISystemSettingService, SystemSettingService>();
builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection(MailSettings.SectionName));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRolePermissionService, RolePermissionService>();
builder.Services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var key = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
{
    throw new InvalidOperationException("Vui lòng cấu hình Jwt:Key trong appsettings.json bằng khóa ngẫu nhiên có ít nhất 32 byte.");
}

var issuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Vui lòng cấu hình Jwt:Issuer trong appsettings.json.");
var audience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Vui lòng cấu hình Jwt:Audience trong appsettings.json.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = "role",
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context => JwtValidationGuard.Run(context, async () =>
            {
                if (!Guid.TryParse(context.Principal?.FindFirst("sub")?.Value, out var id))
                {
                    context.Fail("Thông tin tài khoản trong token không hợp lệ.");
                    return;
                }

                var repository = context.HttpContext.RequestServices.GetRequiredService<IAccountRepository>();
                var account = await repository.GetAsync(id, context.HttpContext.RequestAborted);

                if (account is null || !account.IsActive)
                {
                    context.Fail("Tài khoản không tồn tại hoặc đã bị khóa.");
                    return;
                }

                if (!Guid.TryParse(context.Principal?.FindFirst("sid")?.Value, out var sessionId))
                {
                    context.Fail("Phiên đăng nhập không hợp lệ.");
                    return;
                }

                var authRepository = context.HttpContext.RequestServices.GetRequiredService<IAuthRepository>();
                if (!await authRepository.IsRefreshSessionActiveAsync(sessionId, id, context.HttpContext.RequestAborted))
                {
                    context.Fail("Phiên đăng nhập đã bị thu hồi hoặc hết hạn.");
                    return;
                }

                // Luôn lấy role hiện tại từ database, tránh token cũ giữ quyền sau khi bị hạ quyền.
                if (context.Principal?.Identity is ClaimsIdentity identity)
                {
                    foreach (var claim in identity.FindAll("role").ToList())
                    {
                        identity.RemoveClaim(claim);
                    }

                    identity.AddClaim(new Claim("role", account.Role));
                }
            }),
            OnChallenge = async context =>
            {
                context.HandleResponse();
                if (context.HttpContext.RequestAborted.IsCancellationRequested)
                {
                    context.Response.StatusCode = 499;
                    return;
                }
                context.Response.Headers.WWWAuthenticate = "Bearer";
                await ApiResponseWriter.WriteErrorAsync(context.HttpContext, StatusCodes.Status401Unauthorized);
            },
            OnForbidden = async context =>
            {
                await ApiResponseWriter.WriteErrorAsync(context.HttpContext, StatusCodes.Status403Forbidden);
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in AppPermissions.All)
    {
        options.AddPolicy(permission, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new PermissionRequirement(permission));
        });
    }
});
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        if (corsOrigins.Length == 0)
        {
            return;
        }

        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        await ApiResponseWriter.WriteErrorAsync(
            context.HttpContext,
            StatusCodes.Status429TooManyRequests,
            cancellationToken: cancellationToken);
    };
    options.AddPolicy("auth", context =>
    {
        var address = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(address, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

var app = builder.Build();

app.UseMiddleware<SystemLogMiddleware>();

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseStatusCodePages(async statusContext =>
{
    var context = statusContext.HttpContext;

    if (context.Request.Path.StartsWithSegments("/api"))
    {
        await ApiResponseWriter.WriteErrorAsync(context, context.Response.StatusCode);
    }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("../openapi/v1.json", "Base API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Base API - Swagger";
        options.HeadContent = "<style>.responses-inner > .responses-table, .responses-inner > h4 { display: none; }</style>";
    });
}

app.UseHttpsRedirection();
// Leave certificates and attendance evidence must only be fetched through authorized API routes.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/uploads"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next(context);
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.Combine(webRoot, "uploads")),
    RequestPath = "/uploads",
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Context.Response.Headers["Content-Security-Policy"] = "sandbox; default-src 'none'";

        if (!context.File.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            && !context.File.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            && !context.File.Name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers.ContentDisposition = "attachment";
        }
    }
});
app.UseRateLimiter();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Capture before RunAsync disposes the host, so normal cancellation during shutdown
// does not try to access an already disposed IServiceProvider.
var shutdownToken = app.Lifetime.ApplicationStopping;
try
{
    await app.RunAsync();
}
catch (OperationCanceledException) when (shutdownToken.IsCancellationRequested)
{
    // Expected host shutdown; unrelated startup/runtime errors still propagate.
}

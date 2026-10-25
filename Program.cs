using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.RateLimiting;
using System.Text;
using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuVien.Api;
using ThuVien.Data;
using ThuVien.Models;
using ThuVien.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC + chống CSRF cho mọi request POST/PUT/DELETE
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<AuditFilter>();
});

// Web API: lỗi kiểm tra dữ liệu trả về cùng cấu trúc JSON { success, data, error }
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToList();
        var message = errors.Count > 0 ? string.Join(" ", errors) : "Dữ liệu gửi lên không hợp lệ.";
        return new BadRequestObjectResult(ApiResponse<object>.Fail(message));
    };
});

// Quốc tế hóa: tệp tài nguyên .resx trong thư mục Resources
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Entity Framework Core + SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Băm mật khẩu (PBKDF2) bằng PasswordHasher của ASP.NET Core
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Nghiệp vụ mượn/trả/đặt trước sách
builder.Services.AddScoped<LoanService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddHostedService<ReservationQueueHostedService>();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// JWT cho REST API + Cookie cho giao diện MVC.
// Trong Development, nếu chưa có JWT_SIGNING_KEY thì tạo key tạm cho phiên chạy hiện tại.
// Khi triển khai thật, bắt buộc cấu hình Authentication:Jwt:SigningKey hoặc biến môi trường JWT_SIGNING_KEY.
var configuredJwtKey = builder.Configuration["Authentication:Jwt:SigningKey"]
                       ?? builder.Configuration["JWT_SIGNING_KEY"];
byte[] jwtKeyBytes;
if (string.IsNullOrWhiteSpace(configuredJwtKey))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Thiếu JWT_SIGNING_KEY hoặc Authentication:Jwt:SigningKey cho môi trường không phải Development.");
    jwtKeyBytes = RandomNumberGenerator.GetBytes(64);
    Console.WriteLine("WARNING: JWT signing key tạm thời chỉ dùng cho Development; token sẽ mất hiệu lực khi ứng dụng khởi động lại.");
}
else
{
    jwtKeyBytes = Encoding.UTF8.GetBytes(configuredJwtKey);
    if (jwtKeyBytes.Length < 32)
        throw new InvalidOperationException("JWT signing key phải dài ít nhất 32 byte.");
}
var jwtIssuer = builder.Configuration["Authentication:Jwt:Issuer"] ?? "ThuVien.Api";
var jwtAudience = builder.Configuration["Authentication:Jwt:Audience"] ?? "ThuVien.Client";
var jwtLifetime = int.TryParse(builder.Configuration["Authentication:Jwt:LifetimeMinutes"], out var configuredLifetime)
    ? Math.Clamp(configuredLifetime, 5, 1440)
    : 60;
builder.Services.AddSingleton(new JwtTokenService(jwtKeyBytes, jwtIssuer, jwtAudience, jwtLifetime));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "SmartAuth";
    options.DefaultAuthenticateScheme = "SmartAuth";
    options.DefaultChallengeScheme = "SmartAuth";
})
.AddPolicyScheme("SmartAuth", "Cookie or Bearer", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? JwtBearerDefaults.AuthenticationScheme
            : CookieAuthenticationDefaults.AuthenticationScheme;
    };
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "ThuVien.Auth";
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

    // Kiểm tra tài khoản trên mỗi request: bị khóa hoặc đổi vai trò thì phiên cũ mất hiệu lực.
    options.Events.OnValidatePrincipal = async ctx =>
    {
        var idClaim = ctx.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = ctx.Principal?.FindFirst(ClaimTypes.Role)?.Value;
        var valid = false;

        if (int.TryParse(idClaim, out var userId))
        {
            var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var user = await db.Users.AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new { u.IsActive, u.Role })
                .FirstOrDefaultAsync();
            valid = user != null && user.IsActive && user.Role == roleClaim;
        }

        if (!valid)
        {
            ctx.RejectPrincipal();
            await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    };

    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return ctx.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Bạn chưa đăng nhập."));
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return ctx.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Bạn không có quyền thực hiện thao tác này."));
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.MapInboundClaims = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name
    };
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Bearer token không hợp lệ hoặc đã hết hạn."));
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Bạn không có quyền thực hiện thao tác này."));
        }
    };
});

// Giới hạn thêm theo IP cho endpoint phát hành token.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("TokenIssue", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(15),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("Login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(15),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Tạo CSDL (nếu chưa có) và nạp dữ liệu mẫu
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

    db.Database.EnsureCreated();
    DbBootstrap.EnsureExtras(db);

    // Chỉ tạo tài khoản/sách mẫu trong Development; không tự tạo admin mặc định khi triển khai.
    if (app.Environment.IsDevelopment())
    {
        DbSeeder.Seed(db, hasher);
    }

    // Khi chạy ở chế độ Development: xuất script tạo CSDL ra thư mục Database
    if (app.Environment.IsDevelopment())
    {
        var dir = Path.Combine(app.Environment.ContentRootPath, "Database");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "01_CreateDatabase.sql"), db.Database.GenerateCreateScript());
    }
}

// Một số header bảo mật cơ bản
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/StatusPage", "?code={0}");
app.UseHttpsRedirection();
app.UseStaticFiles();

// Ngôn ngữ giao diện: mặc định tiếng Việt, hỗ trợ tiếng Anh (lưu bằng cookie)
var supportedCultures = new[] { "vi", "en" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("vi")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.OutputCaching;
using System.Threading.RateLimiting;
using Serilog;
using Serilog.Context;
using Store.Data;
using Store.Models;
using Store.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Логирование: Serilog + structured JSON ----------
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext());

// ---------- БД ----------
var conn = builder.Configuration.GetConnectionString("Default") ?? "Data Source=store.db";
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(conn));

// ---------- Identity ----------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(o =>
{
    o.Password.RequiredLength = 6;
    o.Password.RequireNonAlphanumeric = false;
    o.User.RequireUniqueEmail = true;
    o.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
});

// ---------- Сессия (ключ корзины) ----------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(24);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
});

// ---------- Кэш и сервисы ----------
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();

// ---------- Output Caching ----------
builder.Services.AddOutputCache(o =>
{
    o.AddPolicy("catalog", p => p.Expire(TimeSpan.FromSeconds(30)));
});

// ---------- Rate limiting (AJAX) ----------
builder.Services.AddRateLimiter(o =>
{
    o.AddFixedWindowLimiter("ajax", opt =>
    {
        opt.PermitLimit = 30;
        opt.Window = TimeSpan.FromSeconds(10);
        opt.QueueLimit = 0;
    });
    o.RejectionStatusCode = 429;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// ---------- Инициализация БД ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    await DbSeeder.SeedAsync(db);
}

// ---------- Correlation ID (до Serilog RequestLogging) ----------
app.Use(async (ctx, next) =>
{
    var id = ctx.Request.Headers["X-Correlation-ID"].FirstOrDefault()
             ?? Guid.NewGuid().ToString("N");
    ctx.Response.Headers["X-Correlation-ID"] = id;
    using (LogContext.PushProperty("CorrelationId", id))
    {
        await next();
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();     // Все запросы логируются с CorrelationId
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();            // ВАЖНО: до UseAuthorization
app.UseAuthorization();
app.UseOutputCache();
app.UseRateLimiter();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
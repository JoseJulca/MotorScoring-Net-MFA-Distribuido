using Microsoft.AspNetCore.Authentication.Cookies;
using MotorScoring.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<WebSignInService>();
builder.Services.AddScoped<TokenSessionService>();
builder.Services.AddSingleton<QrCodeService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "MotorScoring.Web.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = false;
    });
builder.Services.AddAuthorization();

builder.Services.AddHttpClient<IIdentityApiService, IdentityApiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(configuration["Identity:BaseUrl"]
        ?? throw new InvalidOperationException("No se configuró Identity:BaseUrl."));
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<ISolicitudCreditoApiService, SolicitudCreditoApiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(configuration["MotorScoringApi:BaseUrl"]
        ?? throw new InvalidOperationException("No se configuró MotorScoringApi:BaseUrl."));
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/SolicitudesCredito/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=SolicitudesCredito}/{action=Crear}/{id?}");
app.Run();

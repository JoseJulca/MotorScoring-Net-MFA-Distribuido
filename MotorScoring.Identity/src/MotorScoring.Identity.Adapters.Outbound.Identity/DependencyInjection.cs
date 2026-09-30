using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MotorScoring.Identity.Application.Ports.Out;
using MotorScoring.Identity.Data;
using MotorScoring.Identity.Models;
using MotorScoring.Identity.Security;
using MotorScoring.Identity.Services;

namespace MotorScoring.Identity.Adapters.Outbound.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityOutboundAdapter(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? throw new InvalidOperationException("Configuración Jwt requerida.");
        if (Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32) throw new InvalidOperationException("Jwt:SigningKey debe tener al menos 32 bytes.");

        services.AddDbContext<IdentityDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("IdentityDb")));
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 10;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        var auth = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        })
        .AddCookie(IdentityConstants.ExternalScheme, options =>
        {
            options.Cookie.Name = "MotorScoring.Identity.External";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        });

        var googleId = configuration["Authentication:Google:ClientId"];
        var googleSecret = configuration["Authentication:Google:ClientSecret"];
        if (!string.IsNullOrWhiteSpace(googleId) && !string.IsNullOrWhiteSpace(googleSecret))
            auth.AddGoogle("Google", options => { options.SignInScheme = IdentityConstants.ExternalScheme; options.ClientId = googleId; options.ClientSecret = googleSecret; options.SaveTokens = false; });

        var githubId = configuration["Authentication:GitHub:ClientId"];
        var githubSecret = configuration["Authentication:GitHub:ClientSecret"];
        if (!string.IsNullOrWhiteSpace(githubId) && !string.IsNullOrWhiteSpace(githubSecret))
            auth.AddOAuth("GitHub", options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.ClientId = githubId;
                options.ClientSecret = githubSecret;
                options.CallbackPath = "/signin-github";
                options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                options.TokenEndpoint = "https://github.com/login/oauth/access_token";
                options.UserInformationEndpoint = "https://api.github.com/user";
                options.Scope.Add("read:user");
                options.Scope.Add("user:email");
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                options.ClaimActions.MapJsonKey("urn:github:login", "login");
                options.Events.OnCreatingTicket = async context =>
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                    req.Headers.UserAgent.ParseAdd("MotorScoring.Identity/1.0");
                    using var response = await context.Backchannel.SendAsync(req, context.HttpContext.RequestAborted);
                    response.EnsureSuccessStatusCode();
                    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
                    context.RunClaimActions(doc.RootElement);
                    var email = doc.RootElement.TryGetProperty("email", out var emailElement) ? emailElement.GetString() : null;
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        using var emailReq = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
                        emailReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                        emailReq.Headers.UserAgent.ParseAdd("MotorScoring.Identity/1.0");
                        using var emailResponse = await context.Backchannel.SendAsync(emailReq, context.HttpContext.RequestAborted);
                        if (emailResponse.IsSuccessStatusCode)
                        {
                            using var emailDoc = JsonDocument.Parse(await emailResponse.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
                            var primary = emailDoc.RootElement.EnumerateArray().FirstOrDefault(x => x.TryGetProperty("primary", out var p) && p.GetBoolean() && x.TryGetProperty("verified", out var v) && v.GetBoolean());
                            if (primary.ValueKind != JsonValueKind.Undefined && primary.TryGetProperty("email", out var pe)) email = pe.GetString();
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(email)) context.Identity?.AddClaim(new Claim(ClaimTypes.Email, email));
                };
            });

        services.AddScoped<IIdentityUserPort, IdentityUserAdapter>();
        services.AddScoped<ITokenPort, TokenService>();
        return services;
    }
}

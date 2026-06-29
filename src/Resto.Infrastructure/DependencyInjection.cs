using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Resto.Application.Common.Interfaces;
using Resto.Infrastructure.Events;
using Resto.Infrastructure.Events.Handlers;
using Resto.Infrastructure.Identity;
using Resto.Infrastructure.Persistence;
using Resto.Infrastructure.Persistence.Repositories;
using Resto.Infrastructure.SignalR;

namespace Resto.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexión DefaultConnection es obligatoria.");

        services.AddDbContext<RestoDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<RestoDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, IdentityAuthService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("La configuración JWT es obligatoria.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    RoleClaimType = "role",
                    NameClaimType = JwtRegisteredClaimNames.Name,
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.Authenticated, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(AuthPolicies.WaiterOrManager, policy =>
                policy.RequireRole(AppRoles.Waiter, AppRoles.Manager, AppRoles.Admin))
            .AddPolicy(AuthPolicies.ManagerOnly, policy =>
                policy.RequireRole(AppRoles.Manager, AppRoles.Admin))
            .AddPolicy(AuthPolicies.KitchenOrManager, policy =>
                policy.RequireRole(AppRoles.Kitchen, AppRoles.Manager, AppRoles.Admin))
            .AddPolicy(AuthPolicies.StaffManagement, policy =>
                policy.RequireRole(AppRoles.Manager, AppRoles.Admin));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ITableRepository, TableRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICashRegisterShiftRepository, CashRegisterShiftRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IRestoReadDb, RestoReadDb>();
        services.AddScoped<IEfConcurrencyHelper, EfConcurrencyHelper>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IOrderSentToKitchenEventHandler, OrderSentToKitchenEventHandler>();
        services.AddScoped<IOrderClosedEventHandler, OrderClosedEventHandler>();
        services.AddScoped<ITableStateChangedEventHandler, TableStateChangedEventHandler>();
        services.AddScoped<IRestoNotifier, RestoSignalRNotifier>();

        services.AddSignalR();

        return services;
    }
}

    using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using TicketsDomain.IRepositories;
using TicketsDomain.Models;
using TicketsPerstince.Data;
using TicketsPerstince.Data.DataSeeding;
using TicketsPerstince.Data.DbContexts;
using TicketsPerstince.Repository;
using TicketsServiesAbstraction.IServices;
using TicketsServies;
using TicketsShared.Settings;
    
namespace TiketApp.Api.Extensions
{
    public static class ServiceExtensions
    {
        // 1️⃣ Database
        public static IServiceCollection AddDatabaseServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<DbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());

            return services;
        }

        // 2️⃣ Identity
        public static IServiceCollection AddIdentityServices(
            this IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;

                // ✅ Lockout بعد محاولات فاشلة
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            return services;
        }

        // 3️⃣ JWT Authentication
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
                ?? throw new InvalidOperationException("JwtSettings section is missing in configuration.");

            services.AddSingleton(jwtSettings);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Secret))
                };
            });

            return services;
        }

        // 4️⃣ Repositories
        public static IServiceCollection AddRepositoryServices(
            this IServiceCollection services)
        {
            services.AddScoped(
                typeof(IGenericRepository<,>),
                typeof(GenericRepository<,>));

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

        // 5️⃣ Data Seeding
        public static IServiceCollection AddDataSeeding(
            this IServiceCollection services)
        {
            services.AddScoped<IDataInitializer, DataInitializer>();

            return services;
        }

        // 6️⃣ Application Services
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITicketService, TicketService>();
            services.AddScoped<IDoctorService, DoctorService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();

            return services;
        }

        // 7️⃣ Caching
        public static IServiceCollection AddCachingServices(
            this IServiceCollection services)
        {
            services.AddMemoryCache();
            return services;
        }

        // 8️⃣ Rate Limiting (حماية من DDoS)
        public static IServiceCollection AddRateLimitingServices(
            this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                // ✅ Global — كل IP له حد عام
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    context => RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,              // 100 request
                            Window = TimeSpan.FromMinutes(1), // per minute
                            QueueLimit = 0
                        }));

                // ✅ Login — حماية خاصة من Brute Force
                options.AddFixedWindowLimiter("login", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 5;               // 5 محاولات
                    limiterOptions.Window = TimeSpan.FromMinutes(5); // كل 5 دقايق
                    limiterOptions.QueueLimit = 0;
                });

                // ✅ API — للـ endpoints العادية
                options.AddFixedWindowLimiter("api", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 30;
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.QueueLimit = 2;
                });

                // ✅ الـ Response لما يتجاوز الـ Limit
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsync(
                        """{"success":false,"message":"Too many requests. Please try again later.","errors":[]}""",
                        cancellationToken);
                };
            });

            return services;
        }

        // 9️⃣ CORS
        public static IServiceCollection AddCorsServices(
            this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(
                            "http://localhost:3000",
                            "http://localhost:5173",
                            "http://localhost:5174")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            return services;
        }
    }
}

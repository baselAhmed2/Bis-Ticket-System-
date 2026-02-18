using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
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

            // علشان TicketService يقدر ياخد DbContext
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
                // تخفيف شروط الباسورد علشان الـ SSN (أرقام بس)
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
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
            // Week 2 - Auth
            services.AddScoped<IAuthService, AuthService>();

            // Week 3 - Tickets
            services.AddScoped<ITicketService, TicketService>();

            // Week 4 - Doctor (هتتضاف لاحقاً)
            // services.AddScoped<IDoctorService, DoctorService>();

            // Week 5 - Admin (هتتضاف لاحقاً)
            // services.AddScoped<IAdminService, AdminService>();

            return services;
        }
    }
}

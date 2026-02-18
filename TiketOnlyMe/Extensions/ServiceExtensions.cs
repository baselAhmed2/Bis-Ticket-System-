using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketsDomain.IRepositories;
using TicketsDomain.Models;
using TicketsPerstince.Data;
using TicketsPerstince.Data.DataSeeding;
using TicketsPerstince.Data.DbContexts;
using TicketsPerstince.Repository;

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

            return services;
        }

        // 2️⃣ Identity
        public static IServiceCollection AddIdentityServices(
            this IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            return services;
        }

        // 3️⃣ Repositories
        public static IServiceCollection AddRepositoryServices(
            this IServiceCollection services)
        {
            services.AddScoped(
                typeof(IGenericRepository<,>),
                typeof(GenericRepository<,>));

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

        // 4️⃣ Data Seeding
        public static IServiceCollection AddDataSeeding(
            this IServiceCollection services)
        {
            services.AddScoped<IDataInitializer, DataInitializer>();

            return services;
        }

        // 5️⃣ Application Services (هتتملى مع كل Sprint)
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services)
        {
            // Week 2 - Auth
            // services.AddScoped<IAuthService, AuthService>();

            // Week 3 - Tickets
            // services.AddScoped<ITicketService, TicketService>();
    
            // Week 4 - Doctor
            // services.AddScoped<IDoctorService, DoctorService>();

            // Week 5 - Admin
            // services.AddScoped<IAdminService, AdminService>();

            return services;
        }
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using TicketsDomain.Models;
using TicketsPerstince.Data.DataSeeding;
using TiketApp.Api.Extensions;
using TicketsPerstince.Data.DbContexts;

namespace TiketOnlyMe
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Controllers + Swagger
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Extension Methods
            builder.Services.AddDatabaseServices(builder.Configuration);
            builder.Services.AddIdentityServices();
            builder.Services.AddJwtAuthentication(builder.Configuration);
            builder.Services.AddRepositoryServices();
            builder.Services.AddDataSeeding();
            builder.Services.AddApplicationServices();
            builder.Services.AddCachingServices();

            var app = builder.Build();

            // Database Seeding
            using (var scope = app.Services.CreateScope())
            {
                var initializer = scope.ServiceProvider
                    .GetRequiredService<IDataInitializer>();

                await initializer.InitializeAsync();
            }

            // Swagger
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}

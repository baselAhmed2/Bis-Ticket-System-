using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketsDomain.Models;
using TicketsPerstince.Data.DataSeeding;
using TicketsShared.Common;
using TiketApp.Api.Extensions;
using TiketApp.Api.Middleware;
using TicketsPerstince.Data.DbContexts;
using TiketApp.Api.Controllers;

namespace TiketOnlyMe
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //// ✅ Limit request body size (1MB)
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = 1_048_576; // 1MB
            });

            // Controllers + Swagger + Model Validation
            builder.Services.AddControllers()
                .AddApplicationPart(typeof(AuthController).Assembly)
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var errors = context.ModelState
                            .Where(e => e.Value?.Errors.Count > 0)
                            .SelectMany(e => e.Value!.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList();

                        var response = ApiResponse<object>.ErrorResponse(
                            "Validation failed", errors);

                        return new BadRequestObjectResult(response);
                    };
                });

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
            builder.Services.AddRateLimitingServices();
            builder.Services.AddCorsServices();

            var app = builder.Build();

            // Database Seeding
            //using (var scope = app.Services.CreateScope())
            //{
            //    var initializer = scope.ServiceProvider
            //        .GetRequiredService<IDataInitializer>();

            //    await initializer.InitializeAsync();
            //}

            // ✅ Security Headers
            app.UseMiddleware<SecurityHeadersMiddleware>();

            //// Global Exception Handling
            app.UseMiddleware<GlobalExceptionMiddleware>();

            // Swagger
            //if (app.Environment.IsDevelopment())
            //{
            app.UseSwagger();
                app.UseSwaggerUI();
            //}

            app.UseHttpsRedirection();

            // ✅ CORS (قبل Auth)
            app.UseCors("AllowFrontend");

            // ✅ Rate Limiting (قبل Auth)
            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
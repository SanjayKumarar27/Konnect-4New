using Konnect_4New.Hubs;
using Konnect_4New.Models;
using Microsoft.EntityFrameworkCore;

namespace Konnect_4
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngularApp",
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:4200")
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials(); // Required for SignalR
                    });
            });

            // Add services
            builder.Services.AddControllers();
            builder.Services.AddDbContext<Konnect4Context>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("mycon")));

            // Add SignalR
            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowAngularApp");
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            // Map SignalR Hub
            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }
    }
}
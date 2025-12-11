using ActivityMonitor.Background.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ActivityMonitor.Background.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ActivityMonitor.Background
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // NOTE: Auto-start registration removed - handled by installer's Startup folder shortcut
            // This prevents duplicate entries in Task Manager Startup tab
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Core Services
                    services.AddSingleton<DatabaseService>();
                    services.AddSingleton<ActivityService>();
                    
                    // Register ActivityService as a Hosted Service (runs the loop)
                    services.AddHostedService(provider => provider.GetRequiredService<ActivityService>());
                })
                .ConfigureLogging(logging => 
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddControllers();
                        services.AddCors(options =>
                        {
                            options.AddPolicy("AllowAll",
                                builder =>
                                {
                                    builder.AllowAnyOrigin()
                                           .AllowAnyMethod()
                                           .AllowAnyHeader();
                                });
                        });
                    });
                    
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseCors("AllowAll");
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });

                    // Explicitly configure Kestrel
                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.ListenLocalhost(2369);
                    });
                    webBuilder.UseUrls("http://localhost:2369");
                });
    }
}

using ActivityMonitor.Background.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

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
                    // FIX: Process fails to find wwwroot if Working Directory is System32 (e.g. Start Menu)
                    // We force the content root to be the application directory.
                    webBuilder.UseContentRoot(System.AppDomain.CurrentDomain.BaseDirectory);

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
                        app.UseDefaultFiles(); // Serve index.html by default
                        app.UseStaticFiles();  // Serve files from wwwroot

                        app.UseRouting();
                        app.UseCors("AllowAll");
                        
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            
                            // SPA Fallback: If no controller matches, serve index.html (client-side routing)
                            endpoints.MapFallbackToFile("index.html");
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

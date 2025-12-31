using Karmetric.Background.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Karmetric.Background
{
    public class Program
    {
        private static readonly string MutexId = "Global\\ActivityMonitor_SingleInstance_Mutex";

        public static void Main(string[] args)
        {
            using (var mutex = new System.Threading.Mutex(false, MutexId, out bool createdNew))
            {
                // Logic:
                // 1. If we are the FIRST instance (createdNew):
                //    - If --ui is passed, open browser.
                //    - Start the Service.
                // 2. If we are NOT the first instance:
                //    - If --ui is passed, open browser (to focus/show UI).
                //    - Exit (do not start another service).
                
                bool openUi = args.Contains("--ui");

                if (openUi)
                {
                    OpenBrowser("http://localhost:2369");
                }

                if (!createdNew)
                {
                    // Already running. We just opened the UI (if requested). Now exit.
                    return;
                }

                // If we are here, we are the single instance.
                CreateHostBuilder(args).Build().Run();
            }
        }

        private static void OpenBrowser(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
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

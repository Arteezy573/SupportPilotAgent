using Microsoft.Extensions.Configuration;

namespace SupportPilotAgent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== SupportPilot Agent Web API ===");
            Console.WriteLine("Starting web server...");

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://localhost:5000");
                });
    }
}
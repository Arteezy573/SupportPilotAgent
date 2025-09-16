using Microsoft.Extensions.Configuration;
using SupportPilotAgent.Configuration;
using SupportPilotAgent.Services;

namespace SupportPilotAgent
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalhost", builder =>
                {
                    builder.WithOrigins("http://localhost:3000")
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Register configuration objects
            services.AddSingleton(LoadAzureOpenAIConfig());
            services.AddSingleton(LoadMcpServerConfiguration());

            // Register services
            services.AddSingleton<ResponseFormatterService>();

            // Register the agent initialization service
            services.AddSingleton<AgentInitializationService>();
            services.AddHostedService<AgentInitializationService>(provider =>
                provider.GetRequiredService<AgentInitializationService>());

            // Register SupportPilotAgent factory
            services.AddSingleton<SupportPilotAgent>(provider =>
            {
                var agentService = provider.GetRequiredService<AgentInitializationService>();
                return agentService.Agent;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors("AllowLocalhost");
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Serve static files for React app
            app.UseStaticFiles();
            app.UseDefaultFiles();
        }

        private AzureOpenAIConfig LoadAzureOpenAIConfig()
        {
            var config = new AzureOpenAIConfig();
            var azureOpenAISection = Configuration.GetSection("AzureOpenAI");
            azureOpenAISection.Bind(config);
            return config;
        }

        private Dictionary<string, McpServerConfig> LoadMcpServerConfiguration()
        {
            var mcpServers = new Dictionary<string, McpServerConfig>();
            var mcpServersSection = Configuration.GetSection("McpServers");

            if (mcpServersSection.Exists())
            {
                foreach (var serverSection in mcpServersSection.GetChildren())
                {
                    var serverName = serverSection.Key;
                    var serverConfig = new McpServerConfig();
                    serverSection.Bind(serverConfig);

                    if (!string.IsNullOrEmpty(serverConfig.Command))
                    {
                        mcpServers[serverName] = serverConfig;
                    }
                }
            }

            return mcpServers;
        }
    }
}
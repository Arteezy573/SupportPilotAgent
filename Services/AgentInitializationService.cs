using Microsoft.Extensions.Hosting;
using SupportPilotAgent.Configuration;

namespace SupportPilotAgent.Services
{
    public class AgentInitializationService : IHostedService
    {
        private readonly ILogger<AgentInitializationService> _logger;
        private readonly AzureOpenAIConfig _azureOpenAIConfig;
        private readonly Dictionary<string, McpServerConfig> _mcpServers;
        private SupportPilotAgent? _agent;

        public AgentInitializationService(
            ILogger<AgentInitializationService> logger,
            AzureOpenAIConfig azureOpenAIConfig,
            Dictionary<string, McpServerConfig> mcpServers)
        {
            _logger = logger;
            _azureOpenAIConfig = azureOpenAIConfig;
            _mcpServers = mcpServers;
        }

        public SupportPilotAgent Agent
        {
            get => _agent ?? throw new InvalidOperationException("Agent not initialized. Make sure the service has started.");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Initializing SupportPilot Agent...");

            try
            {
                _agent = await SupportPilotAgent.CreateAsync(_azureOpenAIConfig, _mcpServers);
                _logger.LogInformation("SupportPilot Agent initialized successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize SupportPilot Agent");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Shutting down SupportPilot Agent...");
            _agent?.Dispose();
            return Task.CompletedTask;
        }
    }
}
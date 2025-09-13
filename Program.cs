using Microsoft.Extensions.Configuration;
using SupportPilotAgent;
using SupportPilotAgent.Configuration;

Console.WriteLine("=== SupportPilot Agent ===");
Console.WriteLine("Intelligent assistance for CSS enabling rapid ticket diagnosis");
Console.WriteLine();


// Configuration setup - try to load from file first, then prompt if needed
var (config, mcpServers) = LoadConfigurations();

Console.WriteLine();

// Initialize the agent
var agent = await SupportPilotAgent.SupportPilotAgent.CreateAsync(config, mcpServers);

Console.WriteLine("SupportPilot Agent initialized successfully!");
Console.WriteLine("Type your message and press Enter. Type 'exit' to quit.");
Console.WriteLine();

// Main chat loop
while (true)
{
    Console.Write("You: ");
    var userInput = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(userInput))
    {
        Console.WriteLine("Please enter a message.");
        continue;
    }

    if (userInput.ToLower().Trim() == "exit")
    {
        Console.WriteLine("Thank you for using SupportPilot Agent!");
        break;
    }

    try
    {
        Console.Write("SupportPilot: ");
        var response = await agent.GenerateAsync(userInput);
        Console.WriteLine(response);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.WriteLine();
}

static (AzureOpenAIConfig, Dictionary<string, McpServerConfig>) LoadConfigurations()
{
    var azureConfig = LoadConfiguration();
    var mcpServers = LoadMcpServerConfiguration();
    return (azureConfig, mcpServers);
}

static AzureOpenAIConfig LoadConfiguration()
{
    var config = new AzureOpenAIConfig();
    
    // Try to load from appsettings.json first
    if (File.Exists("appsettings.json"))
    {
        try
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            
            var configuration = configBuilder.Build();
            var azureOpenAISection = configuration.GetSection("AzureOpenAI");
            
            if (azureOpenAISection.Exists())
            {
                azureOpenAISection.Bind(config);
                
                if (!string.IsNullOrEmpty(config.Endpoint) && !string.IsNullOrEmpty(config.DeploymentName))
                {
                    Console.WriteLine("Configuration loaded from appsettings.json");
                    Console.WriteLine($"Endpoint: {config.Endpoint}");
                    Console.WriteLine($"Deployment: {config.DeploymentName}");
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load configuration from appsettings.json: {ex.Message}");
        }
    }
    
    // Interactive setup if file doesn't exist or is incomplete
    Console.WriteLine("Azure OpenAI configuration needed:");
    
    Console.Write("Azure OpenAI Endpoint: ");
    config.Endpoint = Console.ReadLine() ?? "";
    
    Console.Write("API Key: ");
    config.ApiKey = Console.ReadLine() ?? "";
    
    Console.Write("Deployment Name (default: gpt-4): ");
    var deploymentName = Console.ReadLine();
    if (!string.IsNullOrEmpty(deploymentName))
        config.DeploymentName = deploymentName;

    // Optionally save to file
    Console.Write("Save configuration to appsettings.json? (y/n): ");
    var saveConfig = Console.ReadLine()?.ToLower();
    if (saveConfig == "y" || saveConfig == "yes")
    {
        try
        {
            var jsonConfig = $$"""
                {
                  "AzureOpenAI": {
                    "Endpoint": "{{config.Endpoint}}",
                    "ApiKey": "{{config.ApiKey}}",
                    "DeploymentName": "{{config.DeploymentName}}",
                    "ApiVersion": "{{config.ApiVersion}}"
                  }
                }
                """;
            
            File.WriteAllText("appsettings.json", jsonConfig);
            Console.WriteLine("Configuration saved to appsettings.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not save configuration: {ex.Message}");
        }
    }

    return config;
}

static Dictionary<string, McpServerConfig> LoadMcpServerConfiguration()
{
    var mcpServers = new Dictionary<string, McpServerConfig>();
    
    // Try to load MCP servers from appsettings.json
    if (File.Exists("appsettings.json"))
    {
        try
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            
            var configuration = configBuilder.Build();
            var mcpServersSection = configuration.GetSection("McpServers");
            
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
                        Console.WriteLine($"Loaded MCP server configuration: {serverName}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load MCP server configuration: {ex.Message}");
        }
    }
    
    return mcpServers;
}
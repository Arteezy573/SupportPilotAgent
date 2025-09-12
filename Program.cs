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
Console.WriteLine();

// Main command loop
while (true)
{
    Console.WriteLine("Choose an option:");
    Console.WriteLine("1. Analyze email content only");
    Console.WriteLine("2. Full ticket analysis (with Azure DevOps integration)");
    Console.WriteLine("3. Query Azure DevOps work items");
    Console.WriteLine("4. Get work item details");
    Console.WriteLine("5. Exit");
    Console.Write("Enter your choice (1-5): ");

    var choice = Console.ReadLine();

    try
    {
        switch (choice)
        {
            case "1":
                await AnalyzeEmailOnly(agent);
                break;
            case "2":
                await FullTicketAnalysis(agent);
                break;
            case "3":
                await QueryAzureDevOps(agent);
                break;
            case "4":
                await GetWorkItemDetails(agent);
                break;
            case "5":
                Console.WriteLine("Thank you for using SupportPilot Agent!");
                return;
            default:
                Console.WriteLine("Invalid choice. Please try again.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.WriteLine();
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
    Console.Clear();
    Console.WriteLine("=== SupportPilot Agent ===");
}

static async Task AnalyzeEmailOnly(SupportPilotAgent.SupportPilotAgent agent)
{
    Console.WriteLine();
    Console.WriteLine("=== Email Analysis ===");
    Console.Write("Enter the email content to analyze: ");
    var emailContent = Console.ReadLine() ?? "";

    if (string.IsNullOrEmpty(emailContent))
    {
        Console.WriteLine("Email content cannot be empty.");
        return;
    }

    Console.WriteLine();
    Console.WriteLine("Analyzing email content...");
    Console.WriteLine();

    var result = await agent.AnalyzeEmailOnlyAsync(emailContent);
    Console.WriteLine(result);
}

static async Task FullTicketAnalysis(SupportPilotAgent.SupportPilotAgent agent)
{
    Console.WriteLine();
    Console.WriteLine("=== Full Ticket Analysis ===");
    Console.Write("Enter the email content: ");
    var emailContent = Console.ReadLine() ?? "";

    if (string.IsNullOrEmpty(emailContent))
    {
        Console.WriteLine("Email content cannot be empty.");
        return;
    }

    Console.Write("Azure DevOps Organization URL (optional): ");
    var azureDevOpsUrl = Console.ReadLine() ?? "";

    string personalAccessToken = "";
    if (!string.IsNullOrEmpty(azureDevOpsUrl))
    {
        Console.Write("Personal Access Token: ");
        personalAccessToken = Console.ReadLine() ?? "";
    }

    Console.WriteLine();
    Console.WriteLine("Processing support ticket...");
    Console.WriteLine();

    var result = await agent.ProcessSupportTicketAsync(emailContent, azureDevOpsUrl, personalAccessToken);
    Console.WriteLine(result);
}

static async Task QueryAzureDevOps(SupportPilotAgent.SupportPilotAgent agent)
{
    Console.WriteLine();
    Console.WriteLine("=== Azure DevOps Query ===");
    Console.Write("Azure DevOps Organization URL: ");
    var azureDevOpsUrl = Console.ReadLine() ?? "";

    Console.Write("Personal Access Token: ");
    var personalAccessToken = Console.ReadLine() ?? "";

    Console.Write("WIQL Query (default: SELECT * FROM workitems WHERE [Work Item Type] = 'Bug' AND [State] = 'Active'): ");
    var query = Console.ReadLine();
    if (string.IsNullOrEmpty(query))
        query = "SELECT * FROM workitems WHERE [Work Item Type] = 'Bug' AND [State] = 'Active'";

    Console.WriteLine();
    Console.WriteLine("Querying Azure DevOps...");
    Console.WriteLine();

    var result = await agent.QueryAzureDevOpsAsync(azureDevOpsUrl, personalAccessToken, query);
    Console.WriteLine(result);
}

static async Task GetWorkItemDetails(SupportPilotAgent.SupportPilotAgent agent)
{
    Console.WriteLine();
    Console.WriteLine("=== Work Item Details ===");
    Console.Write("Azure DevOps Organization URL: ");
    var azureDevOpsUrl = Console.ReadLine() ?? "";

    Console.Write("Personal Access Token: ");
    var personalAccessToken = Console.ReadLine() ?? "";

    Console.Write("Work Item ID: ");
    if (int.TryParse(Console.ReadLine(), out int workItemId))
    {
        Console.WriteLine();
        Console.WriteLine("Retrieving work item details...");
        Console.WriteLine();

        var result = await agent.GetWorkItemDetailsAsync(azureDevOpsUrl, personalAccessToken, workItemId);
        Console.WriteLine(result);
    }
    else
    {
        Console.WriteLine("Invalid Work Item ID.");
    }
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
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var azureOpenAISection = configBuilder.GetSection("AzureOpenAI");
            azureOpenAISection.Bind(config);

            // Check if required settings are present
            if (!string.IsNullOrEmpty(config.Endpoint) && !string.IsNullOrEmpty(config.ApiKey))
            {
                Console.WriteLine("Configuration loaded from appsettings.json");
                Console.WriteLine($"Endpoint: {config.Endpoint}");
                Console.WriteLine($"Deployment: {config.DeploymentName}");
                return config;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load configuration from appsettings.json: {ex.Message}");
        }
    }

    // Fallback to interactive input
    Console.WriteLine("No valid configuration file found. Please enter your Azure OpenAI configuration:");
    Console.WriteLine();

    Console.Write("Azure OpenAI Endpoint (e.g., https://your-resource.openai.azure.com/): ");
    config.Endpoint = Console.ReadLine() ?? "";

    Console.Write("Azure OpenAI API Key: ");
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

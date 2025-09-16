using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using SupportPilotAgent.Configuration;
using SupportPilotAgent.Services;
using SupportPilotAgent.Plugins.EmailSummaryPlugin;
using ModelContextProtocol.Client;
using Azure.Identity;

namespace SupportPilotAgent
{
    public class SupportPilotAgent : IDisposable
    {
        private ChatCompletionAgent _agent = null!;
        private readonly AzureOpenAIConfig _azureOpenAIConfig;
        private ChatHistoryAgentThread _thread = null!;
        private readonly McpClientService _mcpClientService;
        private readonly Dictionary<string, McpServerConfig> _mcpServers;

        private const string ResponsibleAiText = """
        ALWAYS follow Microsoft's Responsible AI Standard, which includes the following principles: 
        Fairness, Reliability and Safety, Privacy and Security, Inclusiveness, Transparency and Accountability.
        DO NOT address user asks that are clearly unrelated to customer tickets management, root cause analysis, email analysis, trace file analysis, azure data explorer resource management and NEVER address user asks regarding manipulative or unethical behavior.
        Your expertise is strictly limited to customer tickets management, root cause analysis, email analysis, trace file analysis, azure data explorer resource management, troubleshooting, diagnostic topics.
        For questions not related to customer tickets management, root cause analysis, email analysis, trace file analysis, azure data explorer resource management, troubleshooting, diagnostic, simply give a reminder that you are a Support Pilot Agent.
    """;

        private SupportPilotAgent(AzureOpenAIConfig azureOpenAIConfig, Dictionary<string, McpServerConfig>? mcpServers = null)
        {
            _azureOpenAIConfig = azureOpenAIConfig;
            _mcpClientService = new McpClientService();
            _mcpServers = mcpServers ?? new Dictionary<string, McpServerConfig>();
        }

        public static async Task<SupportPilotAgent> CreateAsync(AzureOpenAIConfig azureOpenAIConfig, Dictionary<string, McpServerConfig>? mcpServers = null)
        {
            var agent = new SupportPilotAgent(azureOpenAIConfig, mcpServers);
            agent._agent = await agent.CreateSupportAgentAsync();
            agent._thread = new ChatHistoryAgentThread();
            return agent;
        }

        private async Task<ChatCompletionAgent> CreateSupportAgentAsync()
        {
            // Create kernel with plugins
            var builder = Kernel.CreateBuilder();

            // Add Azure OpenAI connector
            if (string.IsNullOrEmpty(_azureOpenAIConfig.ApiKey))
            {
                // Use DefaultAzureCredential when no API key is provided
                var credential = new DefaultAzureCredential();
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: _azureOpenAIConfig.DeploymentName,
                    endpoint: _azureOpenAIConfig.Endpoint,
                    credential,
                    apiVersion: _azureOpenAIConfig.ApiVersion);
            }
            else
            {
                // Use API key when provided
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: _azureOpenAIConfig.DeploymentName,
                    endpoint: _azureOpenAIConfig.Endpoint,
                    apiKey: _azureOpenAIConfig.ApiKey,
                    apiVersion: _azureOpenAIConfig.ApiVersion);
            }

            var kernel = builder.Build();

            // Add function invocation filter for logging
            kernel.FunctionInvocationFilters.Add(new FunctionInvocationFilter());

            // Load MCP servers from configuration
            if (_mcpServers.Count > 0)
            {
                Console.WriteLine($"Loading {_mcpServers.Count} MCP server(s) from configuration...");
                int totalMcpToolsLoaded = await _mcpClientService.LoadMcpServersAsync(kernel, _mcpServers);
                Console.WriteLine($"Loaded {totalMcpToolsLoaded} MCP tools total from {_mcpServers.Count} server(s).");
            }
            else
            {
                Console.WriteLine("No MCP servers configured in appsettings.json.");
            }

            // Add email summary plugin
            kernel.ImportPluginFromObject(new EmailSummaryPlugin(), "EmailSummary");

            // Create the ChatCompletionAgent with specific instructions
            var agent = new ChatCompletionAgent()
            {
                Name = "SupportPilotAgent",
                Instructions = @"
[DESCRIPTION]
You are SupportPilot, an intelligent CSS (Customer Service & Support) agent designed to enable rapid ticket diagnosis and drastically improve First Day Resolution OKR.

[INSTRUCTIONS]
Your primary responsibilities:
1. Analyze email content from customers or support tickets to extract key information.
2. From key information, provide diagnostic recommendations or solutions.
3. Query Azure Data Explorer for similar incidents when applicable.
4. Query Azure DevOps for related wiki and work items when applicable.
5. Search Microsoft public documentation using keywords when applicable.
6. If provided a trace file, analyze the trace file and extract key information.
7. If provided a error message, search code repository in Azure DevOps for the error message. Using the search results, provide diagnostic recommendations or solutions.
8. If provided a incident id, you must do following steps in sequence:
   - search Azure Data Explorer using the incident id to find the incident details.
   - Using the incident details, find similar incidents.
   - From similar incidents details, provide diagnostic recommendations or solutions.
9. The cluster uri for incident search in Azure Data Explorer is https://icmcluster.kusto.windows.net. The database name is IcMDataWarehouse. The table name is Incidents. The column name for filter is IncidentId.

Guidelines:
- Always be professional and helpful
- Focus on actionable insights and next steps
- Prioritize business-critical issues appropriately
- Use available plugins to gather comprehensive information
- Structure your responses clearly and concisely

[INPUT]
Here is the customer input to analyze:

{{$input}}

[OUTPUT]
Provide a clear, structured response including:
- Key Information extracted
- Diagnostic Recommendations / Solutions
- Suggested Queries (Azure Data Explorer / Azure DevOps / Microsoft Docs)
- Next Steps

" + ResponsibleAiText,
                Kernel = kernel,
                Arguments = new KernelArguments(new PromptExecutionSettings()
                {
#pragma warning disable SKEXP0001  // suppress specific warning
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
#pragma warning restore SKEXP0001   // restore warning
                })
            };

            return agent;
        }

        public async Task<string> GenerateAsync(string userPrompt)
        {
            try
            {
                // Create current user message
                var userMessage = new ChatMessageContent(AuthorRole.User, userPrompt);
                var messages = new List<ChatMessageContent> { userMessage };
                
                // Get the agent's response using the thread
                await foreach (var response in _agent.InvokeAsync(messages, _thread))
                {
                    var result = response.Message.Content ?? "No response generated";
                    return result;
                }

                return "Error: No response received from agent";
            }
            catch (Exception ex)
            {
                return $"Error generating response: {ex.Message}";
            }
        }

        public void Dispose()
        {
            _mcpClientService?.Dispose();
        }
    }
}
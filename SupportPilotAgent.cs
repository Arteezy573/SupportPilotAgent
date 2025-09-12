using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using SupportPilotAgent.Configuration;
using SupportPilotAgent.Services;
using SupportPilotAgent.Plugins.EmailSummaryPlugin;
using SupportPilotAgent.Plugins.McpPlugin;
using ModelContextProtocol.Client;

namespace SupportPilotAgent
{
    public class SupportPilotAgent : IDisposable
    {
        private ChatCompletionAgent _agent = null!;
        private readonly AzureOpenAIConfig _azureOpenAIConfig;
        private readonly ChatHistory _chatHistory;
        private readonly McpClientService _mcpClientService;
        private readonly Dictionary<string, McpServerConfig> _mcpServers;

        private const string ResponsibleAiText = """
        ALWAYS follow Microsoft's Responsible AI Standard, which includes the following principles: 
        Fairness, Reliability and Safety, Privacy and Security, Inclusiveness, Transparency and Accountability.
        DO NOT address user asks that are clearly unrelated to customer tickets management, root cause analysis, email analysis, trace file analysis, and NEVER address user asks regarding manipulative or unethical behavior.
        Your expertise is strictly limited to customer tickets management, root cause analysis, email analysis, trace file analysis, troubleshooting, diagnostic topics.
        For questions not related to customer tickets management, root cause analysis, email analysis, trace file analysis, troubleshooting, diagnostic, simply give a reminder that you are a Support Pilot Agent.
    """;

        private SupportPilotAgent(AzureOpenAIConfig azureOpenAIConfig, Dictionary<string, McpServerConfig>? mcpServers = null)
        {
            _azureOpenAIConfig = azureOpenAIConfig;
            _mcpClientService = new McpClientService();
            _chatHistory = new ChatHistory();
            _mcpServers = mcpServers ?? new Dictionary<string, McpServerConfig>();
        }

        public static async Task<SupportPilotAgent> CreateAsync(AzureOpenAIConfig azureOpenAIConfig, Dictionary<string, McpServerConfig>? mcpServers = null)
        {
            var agent = new SupportPilotAgent(azureOpenAIConfig, mcpServers);
            agent._agent = await agent.CreateSupportAgentAsync();
            return agent;
        }

        private async Task<ChatCompletionAgent> CreateSupportAgentAsync()
        {
            // Create kernel with plugins
            var builder = Kernel.CreateBuilder();

            // Add Azure OpenAI connector
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: _azureOpenAIConfig.DeploymentName,
                endpoint: _azureOpenAIConfig.Endpoint,
                apiKey: _azureOpenAIConfig.ApiKey,
                apiVersion: _azureOpenAIConfig.ApiVersion);

            var kernel = builder.Build();

            // Load MCP servers from configuration
            int totalMcpToolsLoaded = 0;
            if (_mcpServers.Count > 0)
            {
                Console.WriteLine($"Loading {_mcpServers.Count} MCP server(s) from configuration...");
                totalMcpToolsLoaded = await _mcpClientService.LoadMcpServersAsync(kernel, _mcpServers);
                Console.WriteLine($"Loaded {totalMcpToolsLoaded} MCP tools total from {_mcpServers.Count} server(s).");
            }
            
            // If no MCP tools were loaded, fall back to mock Azure DevOps plugin
            if (totalMcpToolsLoaded == 0)
            {
                Console.WriteLine("No MCP tools available, falling back to mock Azure DevOps plugin.");
                kernel.ImportPluginFromObject(new AzureDevOpsMcpPlugin(), "AzureDevOps");
            }

            // Add email summary plugin
            kernel.ImportPluginFromObject(new EmailSummaryPlugin(), "EmailSummary");

            // Create the ChatCompletionAgent with specific instructions
            var agent = new ChatCompletionAgent()
            {
                Name = "SupportPilotAgent",
                Instructions = @"
You are SupportPilot, an intelligent CSS (Customer Service & Support) agent designed to enable rapid ticket diagnosis and drastically improve First Day Resolution OKR.

Your primary responsibilities:
1. Analyze email content from support tickets to extract key information
2. Summarize customer issues and provide diagnostic recommendations
3. Categorize tickets by urgency, type, and required skills
4. Query Azure DevOps for related work items when applicable
5. Provide comprehensive ticket analysis reports

Guidelines:
- Always be professional and helpful
- Focus on actionable insights and next steps
- Prioritize business-critical issues appropriately
- Use available plugins to gather comprehensive information
- Structure your responses clearly and concisely

Available Tools:
- EmailSummary plugin: For analyzing email content, extracting key info, categorizing tickets, and suggesting diagnostics
- AzureDevOps plugin: For connecting to Azure DevOps, querying work items, and retrieving details

When analyzing support tickets:
1. First summarize the email content
2. Extract key customer and technical information  
3. Categorize the ticket appropriately
4. Suggest specific diagnostic steps
5. Query Azure DevOps for related issues if credentials provided
6. Provide a comprehensive analysis with actionable recommendations

" + ResponsibleAiText,
                Kernel = kernel,
                Arguments = new KernelArguments(new PromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };

            return agent;
        }

        public async Task<string> ProcessSupportTicketAsync(string emailContent, string azureDevOpsUrl = "", string personalAccessToken = "")
        {
            try
            {
                // Create a new chat session for this ticket
                var chatHistory = new ChatHistory();
                
                // Build the prompt for comprehensive ticket analysis
                var prompt = $@"
Please analyze this support ticket email and provide a comprehensive analysis.

Email Content:
{emailContent}

Azure DevOps Details (if provided):
- Organization URL: {azureDevOpsUrl}
- Access Token: {(string.IsNullOrEmpty(personalAccessToken) ? "Not provided" : "Provided")}

Please perform the following analysis:
1. Summarize the email content using the SummarizeEmailContent function
2. Extract key information using the ExtractKeyInformation function  
3. Categorize the ticket using the CategorizeTicket function
4. Suggest diagnostic steps using the SuggestDiagnosticSteps function
{(string.IsNullOrEmpty(azureDevOpsUrl) ? "" : "\n5. Connect to Azure DevOps and query related work items using the provided credentials")}

Provide a structured report with all findings and actionable recommendations.
";

                chatHistory.AddUserMessage(prompt);

                // Get the agent's response
                await foreach (var message in _agent.InvokeAsync(chatHistory))
                {
                    return message.Message.Content ?? "No response generated";
                }

                
                return "Error: No response received from agent";
            }
            catch (Exception ex)
            {
                return $"Error processing support ticket: {ex.Message}";
            }
        }

        public async Task<string> QueryAzureDevOpsAsync(string organizationUrl, string personalAccessToken, string query)
        {
            try
            {
                var chatHistory = new ChatHistory();
                var prompt = $@"
Please query Azure DevOps with the following details:
- Organization URL: {organizationUrl}
- Personal Access Token: {personalAccessToken}
- Query: {query}

Steps to perform:
1. First connect to Azure DevOps using ConnectToAzureDevOps function
2. Then execute the work items query using GetWorkItems function
3. Provide the results in a clear format
";
                
                chatHistory.AddUserMessage(prompt);
                
                await foreach (var message in _agent.InvokeAsync(chatHistory))
                {
                    return message.Message.Content ?? "No response generated";
                }
                
                return "Error: No response received from agent";
            }
            catch (Exception ex)
            {
                return $"Error querying Azure DevOps: {ex.Message}";
            }
        }

        public async Task<string> GetWorkItemDetailsAsync(string organizationUrl, string personalAccessToken, int workItemId)
        {
            try
            {
                var chatHistory = new ChatHistory();
                var prompt = $@"
Please retrieve details for a specific work item from Azure DevOps:
- Organization URL: {organizationUrl}
- Personal Access Token: {personalAccessToken}
- Work Item ID: {workItemId}

Steps to perform:
1. First connect to Azure DevOps using ConnectToAzureDevOps function
2. Then get the work item details using GetWorkItemDetails function
3. Provide a formatted summary of the work item information
";
                
                chatHistory.AddUserMessage(prompt);
                
                await foreach (var message in _agent.InvokeAsync(chatHistory))
                {
                    return message.Message.Content ?? "No response generated";
                }
                
                return "Error: No response received from agent";
            }
            catch (Exception ex)
            {
                return $"Error retrieving work item details: {ex.Message}";
            }
        }

        public async Task<string> AnalyzeEmailOnlyAsync(string emailContent)
        {
            try
            {
                var chatHistory = new ChatHistory();
                var prompt = $@"
Please analyze this email content and provide a comprehensive email-only analysis.

Email Content:
{emailContent}

Please perform the following analysis using the EmailSummary plugin functions:
1. Summarize the email content using SummarizeEmailContent function
2. Extract key information using ExtractKeyInformation function
3. Categorize the ticket using CategorizeTicket function
4. Suggest diagnostic steps using SuggestDiagnosticSteps function

Provide the results in a structured format with clear sections for each analysis.
";
                
                chatHistory.AddUserMessage(prompt);
                
                await foreach (var message in _agent.InvokeAsync(chatHistory))
                {
                    return message.Message.Content ?? "No response generated";
                }
                
                return "Error: No response received from agent";
            }
            catch (Exception ex)
            {
                return $"Error analyzing email: {ex.Message}";
            }
        }

        /// <summary>
        /// Configure MCP client to connect to an Azure DevOps MCP server
        /// </summary>
        /// <param name="serverCommand">Path to the MCP server executable</param>
        /// <param name="serverArgs">Arguments to pass to the MCP server</param>
        /// <returns>True if MCP client was configured successfully</returns>
        public async Task<bool> ConfigureMcpClientAsync(string serverCommand, string[] serverArgs)
        {
            try
            {
                var mcpClient = await _mcpClientService.CreateMcpClientAsync(serverCommand, serverArgs);
                var mcpTools = await _mcpClientService.GetToolsAsync(mcpClient);
                
                // Register MCP tools as kernel functions
                _mcpClientService.RegisterMcpToolsAsKernelFunctions(_agent.Kernel, mcpTools, "AzureDevOps");
                
                Console.WriteLine($"MCP Client configured successfully with {mcpTools.Count} tools available.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to configure MCP client: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            _mcpClientService?.Dispose();
        }
    }
}
using Microsoft.SemanticKernel;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using SupportPilotAgent.Configuration;
using System.Diagnostics;

namespace SupportPilotAgent.Services
{
    public class McpClientService : IDisposable
    {
        private readonly List<IMcpClient> _mcpClients = new List<IMcpClient>();
        private bool _disposed;

        public async Task<IMcpClient> CreateMcpClientAsync(string serverCommand, string[] serverArgs)
        {
            try
            {
                var transport = new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = "AzureDevOps",
                    Command = serverCommand,
                    Arguments = serverArgs
                });

                var mcpClient = await McpClientFactory.CreateAsync(
                    clientTransport: transport,
                    clientOptions: new McpClientOptions()
                );

                _mcpClients.Add(mcpClient);
                return mcpClient;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create MCP client: {ex.Message}", ex);
            }
        }

        public async Task<IList<McpClientTool>> GetToolsAsync(IMcpClient mcpClient)
        {
            try
            {
                return await mcpClient.ListToolsAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve MCP tools: {ex.Message}", ex);
            }
        }

        public async Task<int> LoadMcpServersAsync(Kernel kernel, Dictionary<string, McpServerConfig> mcpServers, HashSet<string>? allowedTools = null)
        {
            int totalToolsLoaded = 0;
            
            // Use provided allowed tools, or default to specific tools for token rate limiting
            allowedTools ??= new HashSet<string>
            {
                "wit_my_work_items",
                "wit_get_work_items_batch_by_ids",
                "core_list_projects",
                "wit_get_work_item",
                "wit_create_work_item"
            };
            
            foreach (var serverEntry in mcpServers)
            {
                var serverName = serverEntry.Key;
                var serverConfig = serverEntry.Value;
                
                try
                {
                    Console.WriteLine($"Connecting to MCP server '{serverName}'...");
                    
                    var mcpClient = await CreateMcpClientAsync(serverConfig.Command, serverConfig.Args);
                    var allMcpTools = await GetToolsAsync(mcpClient);
                    
                    // Filter tools to only include allowed ones
                    var filteredTools = allMcpTools.Where(tool => allowedTools.Contains(tool.Name)).ToList();
                    
                    // Log available tools for debugging
                    Console.WriteLine($"Available tools in '{serverName}': {string.Join(", ", allMcpTools.Select(t => t.Name))}");
                    Console.WriteLine($"Allowed tools filter: {string.Join(", ", allowedTools)}");
                    
                    if (filteredTools.Count > 0)
                    {
                        RegisterMcpToolsAsKernelFunctions(kernel, filteredTools, serverName, serverConfig.Description);
                        Console.WriteLine($"Successfully loaded {filteredTools.Count} filtered tools from MCP server '{serverName}' (out of {allMcpTools.Count} available tools)");
                        totalToolsLoaded += filteredTools.Count;
                    }
                    else
                    {
                        Console.WriteLine($"No allowed tools found in MCP server '{serverName}' (checked {allMcpTools.Count} tools)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load MCP server '{serverName}': {ex.Message}");
                    // Continue with other servers even if one fails
                }
            }
            
            return totalToolsLoaded;
        }

        public void RegisterMcpToolsAsKernelFunctions(Kernel kernel, IList<McpClientTool> tools, string pluginName, string description = "")
        {
            try
            {
                if (tools.Count > 0)
                {
                    // Use provided description or default fallback
                    var pluginDescription = !string.IsNullOrEmpty(description) 
                        ? description 
                        : $"MCP server plugin: {pluginName}";
                    
                    kernel.Plugins.AddFromFunctions(pluginName, description: pluginDescription, tools.Select(tool => tool.AsKernelFunction()));
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to register MCP tools as kernel functions: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var mcpClient in _mcpClients)
                {
                    if (mcpClient is IDisposable disposableClient)
                    {
                        disposableClient.Dispose();
                    }
                }
                _mcpClients.Clear();
                _disposed = true;
            }
        }
    }
}
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

        public async Task<int> LoadMcpServersAsync(Kernel kernel, Dictionary<string, McpServerConfig> mcpServers)
        {
            int totalToolsLoaded = 0;
            
            foreach (var serverEntry in mcpServers)
            {
                var serverName = serverEntry.Key;
                var serverConfig = serverEntry.Value;
                
                try
                {
                    Console.WriteLine($"Connecting to MCP server '{serverName}'...");
                    
                    var mcpClient = await CreateMcpClientAsync(serverConfig.Command, serverConfig.Args);
                    var mcpTools = await GetToolsAsync(mcpClient);
                    
                    RegisterMcpToolsAsKernelFunctions(kernel, mcpTools, serverName);
                    
                    Console.WriteLine($"Successfully loaded {mcpTools.Count} tools from MCP server '{serverName}'");
                    totalToolsLoaded += mcpTools.Count;
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
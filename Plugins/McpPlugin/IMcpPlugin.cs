using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SupportPilotAgent.Plugins.McpPlugin
{
    public interface IMcpPlugin
    {
        Task<string> ConnectToAzureDevOpsAsync(string organizationUrl, string personalAccessToken);
        Task<string> GetWorkItemsAsync(string query);
        Task<string> GetWorkItemDetailsAsync(int workItemId);
        Task<string> UpdateWorkItemAsync(int workItemId, string updates);
    }
}
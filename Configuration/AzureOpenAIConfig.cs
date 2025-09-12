namespace SupportPilotAgent.Configuration
{
    public class AzureOpenAIConfig
    {
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = "gpt-4";
        public string ApiVersion { get; set; } = "2024-06-01";
    }
}
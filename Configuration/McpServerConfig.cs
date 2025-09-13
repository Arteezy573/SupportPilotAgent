namespace SupportPilotAgent.Configuration
{
    public class McpServerConfig
    {
        public string Command { get; set; } = string.Empty;
        public string[] Args { get; set; } = Array.Empty<string>();
        public string Description { get; set; } = string.Empty;
    }

    public class McpServersConfig
    {
        public Dictionary<string, McpServerConfig> McpServers { get; set; } = new Dictionary<string, McpServerConfig>();
    }
}
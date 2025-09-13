# SupportPilot Agent

Intelligent assistance for CSS (Customer Service & Support) enabling rapid ticket diagnosis to improve First Day Resolution OKR.

## Overview

SupportPilot Agent is built using the Microsoft Semantic Kernel Agent Framework with ChatCompletionAgent and integrates with Azure OpenAI to provide intelligent analysis of support tickets and Azure DevOps integration.

## Features

- **Conversational AI Interface**: Natural chat-based interaction with proper conversation context and continuity
- **Email Content Analysis**: Summarizes email content, extracts key information, categorizes tickets, and suggests diagnostic steps
- **MCP Server Integration**: Connects to multiple Model Context Protocol (MCP) servers for extensible functionality
- **Azure DevOps Integration**: Real Azure DevOps MCP server integration for work item management
- **Function Invocation Logging**: Real-time logging of all tool calls with execution times and parameters
- **Azure OpenAI Integration**: Uses Azure OpenAI for intelligent processing and analysis with automatic function selection
- **Responsible AI Integration**: Follows Microsoft's Responsible AI Standards with built-in ethical guidelines
- **Configuration-Driven Setup**: Loads MCP servers from appsettings.json similar to Claude Desktop
- **Thread-Based Chat Management**: Maintains conversation context using ChatHistoryAgentThread

## Project Structure

```
SupportPilotAgent/
├── Configuration/
│   ├── AzureOpenAIConfig.cs          # Azure OpenAI configuration
│   └── McpServerConfig.cs            # MCP server configuration models
├── Services/
│   ├── McpClientService.cs           # MCP client management service
│   └── FunctionInvocationFilter.cs   # Function call logging and monitoring
├── Plugins/
│   └── EmailSummaryPlugin/
│       └── EmailSummaryPlugin.cs     # Email analysis plugin
├── SupportPilotAgent.cs              # Main agent class with async factory pattern
├── Program.cs                        # Command line interface with MCP loading
├── SupportPilotAgent.csproj          # Project file with MCP dependencies
├── appsettings.json                  # Configuration file (git-ignored)
├── claude_desktop_config.example.json # Example Claude Desktop MCP config
└── .gitignore                        # Git ignore rules
```

## Prerequisites

- .NET 8.0 or higher
- Azure OpenAI access with API key and endpoint
- Node.js (for MCP servers that require it)
- NPX (comes with Node.js)
- Azure DevOps organization (optional, for Azure DevOps MCP server)
- MCP servers installed as needed (e.g., `@azure-devops/mcp`, `mcp-remote`)

## Setup and Configuration

### Option 1: Configuration File (Recommended)

1. **Clone or download the project**

2. **Configure Azure OpenAI settings**:
   ```bash
   # Copy the example configuration
   cp appsettings.example.json appsettings.json
   
   # Edit appsettings.json with your Azure OpenAI details
   ```
   
   Update `appsettings.json` with your settings:
   ```json
   {
     "AzureOpenAI": {
       "Endpoint": "https://your-resource.openai.azure.com/",
       "ApiKey": "your-api-key-here", 
       "DeploymentName": "gpt-4",
       "ApiVersion": "2024-06-01"
     },
     "McpServers": {
       "azureDevops": {
         "command": "npx",
         "args": ["-y", "@azure-devops/mcp", "msazure"]
       }
     }
   }
   ```

   **Note**: You can comment out or add additional MCP servers as needed. The example shows Azure DevOps integration focused configuration.

3. **Build the project**:
   ```bash
   cd SupportPilotAgent
   dotnet build
   ```

4. **Run the application**:
   ```bash
   dotnet run
   ```

### Option 2: Interactive Configuration

If you don't have a configuration file, the application will prompt you interactively for:
- Azure OpenAI Endpoint (e.g., `https://your-resource.openai.azure.com/`)
- Azure OpenAI API Key  
- Deployment Name (default: `gpt-4`)

The application will offer to save these settings to `appsettings.json` for future use.

**Note**: The `appsettings.json` file is excluded from version control to protect your API keys.

## MCP Server Configuration

The `McpServers` section in `appsettings.json` allows you to configure multiple MCP servers:

### **Configuration Format**
```json
"McpServers": {
  "serverName": {
    "command": "executable-command",
    "args": ["arg1", "arg2", "..."]
  }
}
```

### **Available MCP Servers**
- **`@azure-devops/mcp`**: Official Azure DevOps MCP server
  ```bash
  npx -y @azure-devops/mcp <organization>
  ```
- **`mcp-remote`**: Connect to remote MCP endpoints
  ```bash
  npx -y mcp-remote <endpoint-url>
  ```
- **Custom Servers**: Any MCP-compliant server executable

### **MCP Server Examples**
```json
{
  "McpServers": {
    "azureDevops": {
      "command": "npx",
      "args": ["-y", "@azure-devops/mcp", "your-org"]
    },
    "filesystem": {
      "command": "npx", 
      "args": ["-y", "@modelcontextprotocol/server-filesystem", "/allowed/path"]
    },
    "customServer": {
      "command": "node",
      "args": ["./custom-mcp-server.js"]
    }
  }
}
```

## Usage

The application provides a **conversational AI interface** where you can chat naturally with the SupportPilot Agent:

### **Natural Language Interaction**
Simply type your requests in natural language:
```
You: Can you help me analyze this email about login issues?
SupportPilot: [Analyzes email and provides comprehensive report]

You: Can you also check Azure DevOps for related tickets?
SupportPilot: [Searches Azure DevOps and correlates findings]
```

### **Supported Operations**
- **Email Analysis**: Paste email content for automatic analysis
- **Azure DevOps Integration**: Query work items, retrieve details, search by criteria
- **Root Cause Analysis**: Diagnostic recommendations and troubleshooting steps
- **Ticket Management**: Categorization, priority assessment, and next steps
- **Conversational Context**: Maintains conversation history for follow-up questions

### **Function Invocation Logging**
All tool calls are logged in real-time:
```
[FUNCTION CALL] Invoking: EmailSummary.SummarizeEmailContent
[FUNCTION ARGS] Arguments: emailContent: Customer John Smith...
[FUNCTION RESULT] Completed: EmailSummary.SummarizeEmailContent in 103ms
[FUNCTION OUTPUT] Result: Email Summary: Customer reporting...
```

## Conversational Architecture

SupportPilot Agent uses advanced conversation management for natural interactions:

### **ChatHistoryAgentThread**
- **Conversation Continuity**: Maintains full conversation context across multiple exchanges
- **Thread-Based Memory**: Uses `ChatHistoryAgentThread` to preserve chat history
- **Contextual Responses**: Agent remembers previous questions and provides relevant follow-ups

### **Single GenerateAsync Method**
```csharp
public async Task<string> GenerateAsync(string userPrompt)
{
    // Create current user message
    var userMessage = new ChatMessageContent(AuthorRole.User, userPrompt);
    var messages = new List<ChatMessageContent> { userMessage };
    
    // Get agent response using the thread for context
    await foreach (var response in _agent.InvokeAsync(messages, _thread))
    {
        return response.Message.Content ?? "No response generated";
    }
}
```

### **Function Invocation Filter**
- **Real-Time Logging**: Monitors all function calls with detailed parameters
- **Performance Tracking**: Measures execution time for each tool invocation
- **Error Handling**: Captures and logs function failures with stack traces
- **Debugging Support**: Provides visibility into agent decision-making process

## MCP Server Integration

SupportPilot Agent uses the official Model Context Protocol (MCP) for extensible functionality. The system includes:

### **Real MCP Integration**
- **Multiple Server Support**: Connects to multiple MCP servers simultaneously
- **Configuration-Driven**: Loads servers from `appsettings.json` similar to Claude Desktop
- **Automatic Tool Registration**: MCP server tools are automatically registered as Semantic Kernel functions
- **Error Resilience**: Individual server failures don't prevent other servers from loading

### **Supported MCP Servers**
- **SupportPilot Server**: Custom MCP server for support-specific functions
- **Azure DevOps Server**: Official `@azure-devops/mcp` for work item management
- **Microsoft Docs Server**: `mcp-remote` for accessing Microsoft Learn documentation
- **Custom Servers**: Easy to add new MCP servers via configuration

### **MCP Client Service**
- **Multi-Client Management**: Manages multiple MCP client connections
- **Error Handling**: Individual server failures don't affect others
- **Resource Management**: Proper disposal of MCP client resources
- **Detailed Logging**: Connection status and tool loading information

## Email Summary Plugin Functions

The email summary plugin provides several kernel functions:

- `SummarizeEmailContentAsync`: Creates a summary of email content
- `ExtractKeyInformationAsync`: Extracts customer, contact, and issue information
- `SuggestDiagnosticStepsAsync`: Suggests diagnostic steps based on content
- `CategorizeTicketAsync`: Categorizes tickets by type and urgency

## Sample Output

When analyzing a support ticket, SupportPilot Agent provides structured output including:

```
=== SupportPilot Agent Analysis ===

Email Summary:
- Customer reporting authentication issues with the web portal
- Error occurs specifically during login attempts using SSO
- Issue started 2 days ago and affects multiple users
- Customer has already tried clearing browser cache and cookies
- Priority: High (business critical functionality)
- Recommended next steps: Check SSO configuration and review authentication logs

Key Information Extracted:
Customer: Contoso Corporation
Contact: John Smith (john.smith@contoso.com)
Issue Type: Authentication/Login Problem
Severity: High
Products Affected: Web Portal, SSO Service
Business Impact: Critical - blocking user access
Timeline: Started 2 days ago

Ticket Categorization:
Primary Category: Authentication & Access
Secondary Category: SSO Issues
Product Area: Identity Management
Urgency Level: High
Estimated Resolution Time: 4-8 hours
Required Skills: Authentication, SSO, Identity Services

Suggested Diagnostic Steps:
1. Check SSO service health and status
2. Review authentication logs for error patterns
3. Verify certificate validity for SSO endpoints
4. Test authentication flow with test user account
5. Check for recent configuration changes
6. Validate network connectivity between services
7. Review user permissions and group memberships

=== Recommendation ===
This ticket has been analyzed and categorized. The diagnostic steps above should help with rapid resolution to improve First Day Resolution metrics.
```

## Technologies Used

- **Microsoft Semantic Kernel Agent Framework**: AI agent orchestration with ChatCompletionAgent and ChatHistoryAgentThread
- **Model Context Protocol (MCP)**: Extensible tool integration with multiple MCP servers
- **Azure OpenAI**: AI model integration with gpt-4
- **.NET 8.0**: Runtime platform with async/await patterns
- **C#**: Programming language with modern features
- **ModelContextProtocol NuGet Package**: Official .NET MCP client implementation
- **Function Invocation Filters**: Real-time monitoring and logging of AI function calls

## Responsible AI Implementation

SupportPilot Agent incorporates Microsoft's Responsible AI Standards with the following principles:

- **Fairness**: Provides unbiased analysis regardless of customer background
- **Reliability and Safety**: Focuses strictly on technical support scenarios
- **Privacy and Security**: Protects sensitive information in ticket analysis
- **Inclusiveness**: Accessible and helpful to all support scenarios
- **Transparency and Accountability**: Clear about capabilities and limitations

### Ethical Guidelines

The agent is designed to:
- **Stay in Scope**: Only address customer tickets, root cause analysis, email analysis, trace file analysis, troubleshooting, and diagnostic topics
- **Reject Off-Topic Requests**: Politely redirect non-support related queries
- **Maintain Professional Standards**: Refuse manipulative or unethical requests
- **Provide Clear Boundaries**: Remind users of its role as a Support Pilot Agent when needed

## Future Enhancements

- **Environment Variables Support**: Add support for MCP server environment variables
- **Enhanced Error Recovery**: Automatic MCP server restart and reconnection
- **MCP Server Health Monitoring**: Real-time monitoring of MCP server status
- **Dynamic MCP Loading**: Runtime addition/removal of MCP servers
- **Web UI Interface**: Web-based interface for easier operation
- **Enhanced Logging**: Structured logging with MCP server metrics
- **Additional MCP Servers**: Integration with more official MCP servers
- **Configuration Validation**: Validate MCP server configurations at startup

## License

This project is provided as-is for demonstration and educational purposes.
# SupportPilot Agent

Intelligent assistance for CSS (Customer Service & Support) enabling rapid ticket diagnosis to improve First Day Resolution OKR.

## Overview

SupportPilot Agent is built using the Microsoft Semantic Kernel Agent Framework with ChatCompletionAgent and integrates with Azure OpenAI to provide intelligent analysis of support tickets and Azure DevOps integration.

## Features

- **Email Content Analysis**: Summarizes email content, extracts key information, categorizes tickets, and suggests diagnostic steps
- **Azure DevOps Integration**: Connects to Azure DevOps via MCP plugin to query work items and get detailed information
- **Azure OpenAI Integration**: Uses Azure OpenAI for intelligent processing and analysis with automatic function selection
- **Responsible AI Integration**: Follows Microsoft's Responsible AI Standards with built-in ethical guidelines
- **Command Line Interface**: Interactive CLI for easy operation

## Project Structure

```
SupportPilotAgent/
├── Configuration/
│   └── AzureOpenAIConfig.cs          # Azure OpenAI configuration
├── Plugins/
│   ├── McpPlugin/
│   │   ├── IMcpPlugin.cs             # MCP plugin interface
│   │   └── AzureDevOpsMcpPlugin.cs   # Azure DevOps MCP implementation
│   └── EmailSummaryPlugin/
│       └── EmailSummaryPlugin.cs     # Email analysis plugin
├── SupportPilotAgent.cs              # Main agent class
├── Program.cs                        # Command line interface
├── SupportPilotAgent.csproj          # Project file
├── appsettings.example.json          # Configuration template
└── .gitignore                        # Git ignore rules
```

## Prerequisites

- .NET 8.0 or higher
- Azure OpenAI access with API key and endpoint
- Azure DevOps organization (optional, for full integration)

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
     }
   }
   ```

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

## Usage

The application provides several operation modes:

### 1. Email Analysis Only
Analyzes email content and provides:
- Email summary
- Key information extraction
- Ticket categorization
- Diagnostic step suggestions

### 2. Full Ticket Analysis (with Azure DevOps)
Performs email analysis plus Azure DevOps integration:
- All email analysis features
- Connection to Azure DevOps
- Query related work items
- Comprehensive support ticket report

### 3. Azure DevOps Query
Directly query Azure DevOps work items using WIQL (Work Item Query Language)

### 4. Work Item Details
Retrieve detailed information about specific work items

## MCP Plugin Architecture

The Azure DevOps MCP plugin is designed for future compatibility with official .NET MCP support. Currently includes:

- **Connection Management**: Handles Azure DevOps organization connections
- **Work Item Querying**: WIQL-based work item retrieval
- **Work Item Details**: Detailed information retrieval
- **Work Item Updates**: Update work item information

*Note: The MCP implementation currently uses mock data as .NET MCP documentation is not yet available. This will be updated once official .NET MCP support is released.*

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

- **Microsoft Semantic Kernel Agent Framework**: AI agent orchestration with ChatCompletionAgent
- **Azure OpenAI**: AI model integration with gpt-4
- **.NET 8.0**: Runtime platform
- **C#**: Programming language

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

- Integration with official .NET MCP support when available
- Enhanced Azure DevOps work item operations
- Additional AI models support
- Web UI interface
- Configuration file support
- Logging and monitoring capabilities

## License

This project is provided as-is for demonstration and educational purposes.
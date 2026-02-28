using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AzureFunctions;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using OpenAI.Chat;
using System;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "gpt-4o-mini";
var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");

AzureOpenAIClient client;

if (!string.IsNullOrEmpty(tenantId))
{
    // Configure DefaultAzureCredential with the specific tenant ID to avoid token mismatch
    var credentialOptions = new DefaultAzureCredentialOptions();
    credentialOptions.TenantId = tenantId;
    client = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential(credentialOptions));

}

else
{
    client = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
}

// Create an AI agent following the standard Microsoft Agent Framework pattern
AIAgent foodAgent = client
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are informative but you compare things to foods.",
        name: "Food");

AIAgent carAgent = client
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are informative but you compare things to cars.",
        name: "Car");

// Configure the function app to host the agent with durable thread management
// This automatically creates HTTP endpoints and manages state persistence
using IHost app = FunctionsApplication
    .CreateBuilder(args)
    .ConfigureFunctionsWebApplication()
    .ConfigureDurableAgents(options =>
        options.AddAIAgent(foodAgent)
        .AddAIAgent(carAgent)
    )
    .Build();
app.Run();
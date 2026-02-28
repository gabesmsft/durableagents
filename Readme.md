# Durable task extension for Microsoft Agent Framework - simple example with two agents

This .NET example uses the [Durable task extension for Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/integrations/azure-functions?pivots=programming-language-csharp&tabs=bash) to run two agents in a Durable Functions orchestrator, based on user input.
This example borrows from the GitHub examples found [here](https://github.com/Azure-Samples/durable-task-extension-for-agent-framework).

This is not a definitive guide for using Microsoft Foundry, OpenAI, Durable Functions, or other technologies. Nor is it prescriptive guiance about setting RBAC roles or coding best-practices. Use your best judgment about setting RBAC roles and running code.

## Prerequisites
1. [Deploy an Azure OpenAI instance](https://learn.microsoft.com/azure/foundry/what-is-foundry), which you can do as a standalone resource or as part of Microsoft Foundry.
2. Assign yourself as a Cognitive Services Contributor to the Foundry or Open AI resource, and then [deploy a model](https://learn.microsoft.com/azure/foundry/foundry-models/how-to/deploy-foundry-models)
   > Note: If using Foundry, this option can also currently be found under the [Models + endpoints](https://ai.azure.com/resource/deployments) section of the Foundry portal, if you don't select the New Foundry toggle.
3. If testing in a Function App, add a role assignment for the Function App's managed identity as a Cognitive Services OpenAI on the Foundry or OpenAI resource.
   > Note: Although secret-based authentication is possible with Azure OpenAI, this sample code instead depends on RBAC access to the Azure OpenAI via your user account (if testing locally) or via the Function App's managed identity (if testing in an Azure Function App).
4. Set the environment variables for the following, in the local.settings.json or in the Function App's environment variables:
    - AZURE_OPENAI_ENDPOINT: This resembles https://YOUROPENAIServerName.openai.azure.com/ . If using Foundry, this can be found on the [overview](https://ai.azure.com/foundryResource/overview) page of the Foundry portal. Currently, there is an Endpoints + Keys section near the top of the page, and a tab for Azure OpenAI which contains the Azure OpenAI endpoint.
    - AZURE_OPENAI_DEPLOYMENT: The name of the model you deployed.
    - AZURE_TENANT_ID: Optional, but may be required in some cases. This is the tenant ID of your Azure Active Directory, which can be found in the Azure portal under Azure Active Directory > Overview > Tenant ID.

## About the code
There are two Microsoft.Agents.AI agents, defined in Program.cs. One agent uses food references to provides answers, and the othr agent uses car references to provide answers.
These agents are added to ConfigureDurableAgents so that the Durable functions can use the agents.

The StartOrchestrationAsync HTTP trigger accepts user input and sends it to the OrchestrationTrigger to be processed by the agents. The route for this trigger is POST /userprompt/orchestration/start

The OrchestrationTrigger in the FunctionsTrigger.cs processes and stores the agents' responses, comparably to how it would process other durable orchestrations, but in the context of agents.

## Test the code
1. Build and run the project locally.
2. Send a POST request to the /userprompt/orchestration/start, with some user prompt text for the agents. E.g.:

```
curl -X POST http://localhost:7071/api/userprompt/orchestration/start -H "Content-Type: text/plain" -d "Tell me the meaning of life" --verbose
```

The response will include an instanceId for the orchestration that was started. You can use this instanceId to check the status of the orchestration and see the agents' responses.

3. Check the agent responses via browser or via a GET request to http://localhost:7071/api/orchestration/status/{instanceId}

The stored response should include the food agent's interpretation and the car agent's interpretation. The presence of the stored response demonstrates persistance via Durable functions.

If you test in a Function App, replace http://localhost:7071 with the Function App's base URL.

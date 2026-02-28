using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DurableTask;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using System.Net;
using System.Text.Json;


public static class FunctionTriggers
{
    public sealed record TextResponse(string Text);

    [Function(nameof(RunOrchestrationAsync))]
    public static async Task<string> RunOrchestrationAsync([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        string userPrompt = context.GetInput<string>() ?? string.Empty;

        DurableAIAgent foodAgent = context.GetAgent("Food");
        AgentSession foodSession = await foodAgent.CreateSessionAsync();

        AgentResponse<TextResponse> foodResponse = await foodAgent.RunAsync<TextResponse>(
            message: userPrompt,
            session: foodSession);

        DurableAIAgent carAgent = context.GetAgent("Car");
        AgentSession carSession = await carAgent.CreateSessionAsync();

        AgentResponse<TextResponse> carResponse = await foodAgent.RunAsync<TextResponse>(
            message: userPrompt,
            session: carSession);

        return $"According to the food agent: {foodResponse.Result.Text}, and according to the car agent: { carResponse.Result.Text} ";
    }


    // POST /singleagent/run
    [Function(nameof(StartOrchestrationAsync))]
    public static async Task<HttpResponseData> StartOrchestrationAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "userprompt/orchestration/start")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        CancellationToken cancellationToken)
    {

        // Read the prompt from the request body
        string userPrompt = await new StreamReader(req.Body).ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            HttpResponseData badrequest = req.CreateResponse(HttpStatusCode.BadRequest);
            return badrequest;
        }

        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            orchestratorName: nameof(RunOrchestrationAsync), userPrompt);

        HttpResponseData response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new
        {
            message = "Orchestration started.",
            instanceId,
            statusQueryGetUri = GetStatusQueryGetUri(req, instanceId),
        });
        return response;
    }

    // GET /singleagent/status/{instanceId}
    [Function(nameof(GetOrchestrationStatusAsync))]
    public static async Task<HttpResponseData> GetOrchestrationStatusAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orchestration/status/{instanceId}")] HttpRequestData req,
        string instanceId,
        [DurableClient] DurableTaskClient client)
    {
        OrchestrationMetadata? status = await client.GetInstanceAsync(
            instanceId,
            getInputsAndOutputs: true,
            req.FunctionContext.CancellationToken);

        if (status is null)
        {
            HttpResponseData notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "Instance not found" });
            return notFound;
        }

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            instanceId = status.InstanceId,
            runtimeStatus = status.RuntimeStatus.ToString(),
            input = status.SerializedInput is not null ? (object)status.ReadInputAs<JsonElement>() : null,
            output = status.SerializedOutput is not null ? (object)status.ReadOutputAs<JsonElement>() : null,
            failureDetails = status.FailureDetails
        });
        return response;
    }

    private static string GetStatusQueryGetUri(HttpRequestData req, string instanceId)
    {
        // NOTE: This can be made more robust by considering the value of
        //       request headers like "X-Forwarded-Host" and "X-Forwarded-Proto".
        string authority = $"{req.Url.Scheme}://{req.Url.Authority}";
        return $"{authority}/api/orchestration/status/{instanceId}";
    }
}

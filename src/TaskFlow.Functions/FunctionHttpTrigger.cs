using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace TaskFlow.Functions;

public class FunctionHttpTrigger(ILogger<FunctionHttpTrigger> logger)
{
    [Function("HealthCheck")]
    public HttpResponseData HealthCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        logger.LogInformation("Health check requested at {UtcNow}", DateTime.UtcNow);
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        response.WriteString("Healthy");
        return response;
    }

    [Function("TaskApiProxy")]
    public async Task<HttpResponseData> TaskApiProxy(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tasks")] HttpRequestData req,
        CancellationToken ct)
    {
        logger.LogInformation("TaskApiProxy invoked at {UtcNow}", DateTime.UtcNow);

        // Read-only proxy: forwards to the task service for lightweight queries
        // Future: wire to ITaskItemService.SearchAsync for direct read access
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { message = "TaskApiProxy placeholder — wire to ITaskItemService" }, ct);
        return response;
    }
}

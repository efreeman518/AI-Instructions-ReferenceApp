using EF.Common.Contracts;
using NBomber.Contracts;
using NBomber.CSharp;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;
using Test.Support;

[assembly: DoNotParallelize]

namespace Test.Load;

/// <summary>
/// NBomber load scenarios against the TaskItem search and CRUD endpoints, asserting success-rate and
/// P99-latency baselines.
/// Load tier (NBomber): both methods are <c>[Ignore]</c>'d for CI and require a running
/// <c>taskflowapi</c> endpoint. Faster tiers (Endpoint/E2E) cannot reproduce the concurrent-load
/// behavior these baselines guard.
/// Manual run:
/// 1. Remove or comment the <c>[Ignore("Run manually - requires API host running")]</c>
///    attribute on the load test method to run.
/// 2. From repo root, start the Aspire host:
///    <c>dotnet run --project src\Host\Aspire\AppHost\AppHost.csproj</c>.
/// 3. Use the <c>taskflowapi</c> HTTP endpoint from Aspire. Default is <c>http://localhost:5188</c>.
///    If Aspire assigns another port, set <c>TASKFLOW_LOAD_BASE_URL</c> before running tests.
/// 4. From the same repo root run:
///    <c>$env:TASKFLOW_LOAD_BASE_URL="http://localhost:5188"; dotnet test src\Test\Test.Load\Test.Load.csproj --filter TestCategory=Load</c>.
/// Output is saved under <c>src\Test\Test.Load\load-reports</c>.
/// Default simulations stay below the API's 100 request/minute tenant rate limit. Raise
/// <c>RateLimiting:PerTenant:PermitLimit</c> before using higher injection profiles.
/// </summary>
[TestClass]
[TestCategory("Load")]
public class TaskItemLoadTests
{
    private const string DefaultBaseUrl = "http://localhost:5188";
    private static readonly string BaseUrl = Environment.GetEnvironmentVariable("TASKFLOW_LOAD_BASE_URL") ?? DefaultBaseUrl;
    private const string TaskItemsPath = "/api/v1/task-items";
    private const string TaskItemsSearchPath = "/api/v1/task-items/search";
    private static readonly string ReportFolder = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "load-reports"));
    private static readonly JsonSerializerOptions JsonOptions = JsonTestOptions.Default;

    /// <summary>Verifies that given task item search endpoint, when load applied, then meets performance baseline.</summary>
    [TestMethod]
    [Ignore("Run manually - requires API host running")]
    public void Given_TaskItemSearchEndpoint_When_LoadApplied_Then_MeetsPerformanceBaseline()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        var scenario = Scenario.Create("task-item-search", async context =>
        {
            var response = await TaskItemLoadStep.RunAsync<SearchRequest<TaskItemSearchFilter>, PagedResponse<TaskItemDto>>(
                context,
                httpClient,
                "search",
                HttpMethod.Post,
                _ => TaskItemsSearchPath,
                _ => new SearchRequest<TaskItemSearchFilter>
                {
                    PageIndex = 0,
                    PageSize = 20,
                    Filter = new TaskItemSearchFilter()
                },
                HttpStatusCode.OK);

            return response;
        })
        // Warms up search before collecting measurements so cold-start work is not counted.
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        // Starts one new search invocation per second for 30 seconds.
        .WithLoadSimulations(
            Simulation.Inject(rate: 1, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );

        // RegisterScenarios gives NBomber the scenario definition to execute.
        // WithReportFolder writes generated NBomber reports under src\Test\Test.Load\load-reports.
        // Run starts the load test and returns aggregate stats for assertions.
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportFolder)
            .Run();

        var scenarioStats = stats.ScenarioStats[0];
        Assert.IsGreaterThanOrEqualTo(95.0, scenarioStats.Ok.Request.Percent,
            $"Success rate {scenarioStats.Ok.Request.Percent}% below 95% threshold");
        Assert.IsLessThan(2000.0, scenarioStats.Ok.Latency.Percent99,
            $"P99 latency {scenarioStats.Ok.Latency.Percent99}ms exceeds 2000ms threshold");
    }

    /// <summary>Verifies that given task item CRUD workflow, when load applied, then meets throughput baseline.</summary>
    [TestMethod]
    [Ignore("Run manually - requires API host running")]
    public void Given_TaskItemCrudWorkflow_When_LoadApplied_Then_MeetsThroughputBaseline()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        var scenario = Scenario.Create("task-item-crud-workflow", async context =>
        {
            var search = await TaskItemLoadStep.RunAsync<SearchRequest<TaskItemSearchFilter>, PagedResponse<TaskItemDto>>(
                context,
                httpClient,
                "search-before-create",
                HttpMethod.Post,
                _ => TaskItemsSearchPath,
                _ => new SearchRequest<TaskItemSearchFilter>
                {
                    PageIndex = 0,
                    PageSize = 10,
                    Filter = new TaskItemSearchFilter()
                },
                HttpStatusCode.OK);
            if (search.IsError) return TaskItemLoadStep.FailScenario(search);

            var create = await TaskItemLoadStep.RunAsync<DefaultRequest<TaskItemDto>, DefaultResponse<TaskItemDto>>(
                context,
                httpClient,
                "create",
                HttpMethod.Post,
                _ => TaskItemsPath,
                CreateTaskItemRequest,
                HttpStatusCode.Created);
            if (create.IsError) return TaskItemLoadStep.FailScenario(create);

            var get = await TaskItemLoadStep.RunAsync<object, DefaultResponse<TaskItemDto>>(
                context,
                httpClient,
                "get",
                HttpMethod.Get,
                ctx => $"{TaskItemsPath}/{TaskItemLoadStep.RequireItem(ctx, "create").Id}",
                payloadBuilder: null,
                HttpStatusCode.OK);
            if (get.IsError) return TaskItemLoadStep.FailScenario(get);

            var update = await TaskItemLoadStep.RunAsync<DefaultRequest<TaskItemDto>, DefaultResponse<TaskItemDto>>(
                context,
                httpClient,
                "update",
                HttpMethod.Put,
                ctx => $"{TaskItemsPath}/{TaskItemLoadStep.RequireItem(ctx, "get").Id}",
                UpdateTaskItemRequest,
                HttpStatusCode.OK);
            if (update.IsError) return TaskItemLoadStep.FailScenario(update);

            var delete = await TaskItemLoadStep.RunAsync<object, object>(
                context,
                httpClient,
                "delete",
                HttpMethod.Delete,
                ctx => $"{TaskItemsPath}/{TaskItemLoadStep.RequireItem(ctx, "update").Id}",
                payloadBuilder: null,
                HttpStatusCode.NoContent);
            if (delete.IsError) return TaskItemLoadStep.FailScenario(delete);

            var verifyDeleted = await TaskItemLoadStep.RunAsync<object, object>(
                context,
                httpClient,
                "verify-deleted",
                HttpMethod.Get,
                ctx => $"{TaskItemsPath}/{TaskItemLoadStep.RequireItem(ctx, "update").Id}",
                payloadBuilder: null,
                HttpStatusCode.NotFound);

            return verifyDeleted.IsError
                ? TaskItemLoadStep.FailScenario(verifyDeleted)
                : Response.Ok();
        })
        // Warms up the full CRUD workflow before collecting steady-state measurements.
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        // RampingInject increases arrival pressure gradually, then Inject holds a steady rate.
        // These rates stay below the default per-tenant API rate limit.
        .WithLoadSimulations(
            Simulation.RampingInject(rate: 1, interval: TimeSpan.FromSeconds(5), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 1, interval: TimeSpan.FromSeconds(5), during: TimeSpan.FromSeconds(60))
        );

        // RegisterScenarios gives NBomber the scenario definition to execute.
        // WithReportFolder writes generated NBomber reports under src\Test\Test.Load\load-reports.
        // Run starts the load test and returns aggregate stats for assertions.
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportFolder)
            .Run();

        var scenarioStats = stats.ScenarioStats[0];
        Assert.IsGreaterThanOrEqualTo(90.0, scenarioStats.Ok.Request.Percent,
            $"CRUD workflow success rate {scenarioStats.Ok.Request.Percent}% below 90% threshold");
    }

    /// <summary>Creates task item request used by the surrounding test cases.</summary>
    private static DefaultRequest<TaskItemDto> CreateTaskItemRequest(IScenarioContext context)
    {
        var name = $"Load test task {context.ScenarioInfo.InstanceNumber}-{context.InvocationNumber}-{Guid.NewGuid():N}";
        return new DefaultRequest<TaskItemDto>
        {
            Item = new TaskItemDto
            {
                Title = name,
                Description = "Generated by load test",
                Priority = Priority.Medium,
                Status = TaskItemStatus.Open
            }
        };
    }

    /// <summary>Verifies update task item request behavior and protects the expected test contract.</summary>
    private static DefaultRequest<TaskItemDto> UpdateTaskItemRequest(IScenarioContext context)
    {
        var current = TaskItemLoadStep.RequireItem(context, "get");
        return new DefaultRequest<TaskItemDto>
        {
            Item = current with
            {
                Title = $"{current.Title} updated",
                Priority = Priority.High,
                Status = TaskItemStatus.InProgress
            }
        };
    }

    /// <summary>Supports test execution for Test.load scenarios.</summary>
    private static class TaskItemLoadStep
    {
        /// <summary>Verifies run behavior and protects the expected test contract.</summary>
        public static async Task<Response<TResponse?>> RunAsync<TRequest, TResponse>(
            IScenarioContext context,
            HttpClient httpClient,
            string stepName,
            HttpMethod method,
            Func<IScenarioContext, string> pathBuilder,
            Func<IScenarioContext, TRequest>? payloadBuilder,
            HttpStatusCode expectedStatusCode)
        {
            return await Step.Run<TResponse?>(stepName, context, async () =>
            {
                try
                {
                    using var request = new HttpRequestMessage(method, pathBuilder(context));
                    var payload = payloadBuilder is null ? default : payloadBuilder(context);
                    if (payload is not null)
                    {
                        request.Content = JsonContent.Create(payload, options: JsonOptions);
                    }

                    using var responseMessage = await httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        context.ScenarioCancellationToken);

                    var body = await responseMessage.Content.ReadAsStringAsync(context.ScenarioCancellationToken);
                    var bodySize = Encoding.UTF8.GetByteCount(body);
                    var response = Deserialize<TResponse>(body);
                    var statusCode = ((int)responseMessage.StatusCode).ToString();

                    if (responseMessage.StatusCode == expectedStatusCode)
                    {
                        if (response is not null)
                        {
                            context.Data[stepName] = response;
                        }

                        return Response.Ok(response, statusCode: statusCode, sizeBytes: bodySize);
                    }

                    return Response.Fail<TResponse?>(
                        message: BuildFailureMessage(responseMessage, body, expectedStatusCode),
                        statusCode: statusCode,
                        sizeBytes: bodySize);
                }
                catch (OperationCanceledException) when (context.ScenarioCancellationToken.IsCancellationRequested)
                {
                    return Response.Fail<TResponse?>("Scenario cancelled", "cancelled", 0);
                }
                catch (Exception ex)
                {
                    return Response.Fail<TResponse?>(ex.Message, "exception", 0);
                }
            });
        }

        /// <summary>Verifies require item behavior and protects the expected test contract.</summary>
        public static TaskItemDto RequireItem(IScenarioContext context, string stepName)
        {
            if (context.Data.TryGetValue(stepName, out var value)
                && value is DefaultResponse<TaskItemDto> { Item: { } item })
            {
                return item;
            }

            throw new InvalidOperationException($"Step '{stepName}' did not capture a TaskItem response.");
        }

        /// <summary>Verifies fail scenario behavior and protects the expected test contract.</summary>
        public static Response<object> FailScenario(IResponse response) =>
            Response.Fail(response.Message, response.StatusCode, response.SizeBytes);

        /// <summary>Verifies deserialize behavior and protects the expected test contract.</summary>
        private static T? Deserialize<T>(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(body, JsonOptions);
            }
            catch (JsonException)
            {
                return default;
            }
        }

        /// <summary>Builds failure message used by focused test cases.</summary>
        private static string BuildFailureMessage(
            HttpResponseMessage response,
            string body,
            HttpStatusCode expectedStatusCode)
        {
            var reason = string.IsNullOrWhiteSpace(response.ReasonPhrase)
                ? "Unexpected status code"
                : response.ReasonPhrase;

            return string.IsNullOrWhiteSpace(body)
                ? $"{reason}. Expected {(int)expectedStatusCode}, got {(int)response.StatusCode}."
                : $"{reason}. Expected {(int)expectedStatusCode}, got {(int)response.StatusCode}. Body: {body}";
        }
    }
}

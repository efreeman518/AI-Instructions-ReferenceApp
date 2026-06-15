using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Infrastructure.AI.Demos;

/// <summary>
/// D6 - Asynchronous, event-driven inference. Invoked from the Functions Service Bus pipeline after a
/// task is created: the model reviews the new task and posts clarifying questions / missing-detail
/// notes as a comment. The inference happens off the request path and produces a side effect on a
/// different surface (a comment), distinct from the synchronous triage (D4) and draft (D5) demos.
/// </summary>
public interface IAiTaskReviewer
{
    /// <summary>Reviews a newly created task and, if it is not already clear, posts a comment.</summary>
    Task ReviewNewTaskAsync(Guid taskId, Guid tenantId, CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class AiTaskReviewer(
    ILogger<AiTaskReviewer> logger,
    IChatClient chatClient,
    ITaskItemService taskItemService) : IAiTaskReviewer
{
    private const string ReadyMarker = "READY";

    /// <inheritdoc />
    public async Task ReviewNewTaskAsync(Guid taskId, Guid tenantId, CancellationToken ct = default)
    {
        if (chatClient is NoOpChatClient)
        {
            logger.LogDebug("AiTaskReviewer skipped for {TaskId} - no model configured.", taskId);
            return;
        }

        var getResult = await taskItemService.GetAsync(taskId);
        if (getResult.IsNone || getResult.IsFailure)
        {
            logger.LogDebug("AiTaskReviewer could not load {TaskId}.", taskId);
            return;
        }

        var task = getResult.Value!.Item!;

        var prompt = $$"""
            A new task was just created. Review it for readiness. If important details are missing or
            ambiguous, list 2-3 short clarifying questions a reviewer should resolve before work starts.
            If the task is already clear and actionable, reply with exactly: {{ReadyMarker}}
            Reply in plain text, no markdown.
            Title: {{task.Title}}
            Description: {{task.Description ?? "(none)"}}
            """;

        var response = await chatClient.GetResponseAsync(prompt, cancellationToken: ct);
        var text = response.Text?.Trim() ?? string.Empty;

        if (text.Length == 0 || text.Equals(ReadyMarker, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("AiTaskReviewer found {TaskId} ready; no comment posted.", taskId);
            return;
        }

        var comment = new CommentDto
        {
            TenantId = tenantId,
            TaskItemId = taskId,
            Body = $"AI readiness review:\n{text}"
        };

        var result = await taskItemService.AddCommentAsync(taskId, comment, ct);
        if (result.IsFailure)
            logger.LogWarning("AiTaskReviewer failed to post comment on {TaskId}: {Error}", taskId, result.ErrorMessage);
        else
            logger.LogInformation("AiTaskReviewer posted a readiness comment on {TaskId}.", taskId);
    }
}

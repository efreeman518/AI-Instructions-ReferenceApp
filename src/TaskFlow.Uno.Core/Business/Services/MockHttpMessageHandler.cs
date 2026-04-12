using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskFlow.Uno.Core.Client;

namespace TaskFlow.Uno.Core.Business.Services;

/// <summary>
/// Returns canned mock data for all API endpoints when USE_MOCKS is enabled.
/// Routes are matched by path prefix; responses are deterministic.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private static readonly Guid _cat1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _cat2Id = Guid.Parse("11111111-1111-1111-1111-222222222222");
    private static readonly Guid _tag1Id = Guid.Parse("22222222-2222-2222-2222-111111111111");
    private static readonly Guid _tag2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid _task1Id = Guid.Parse("33333333-3333-3333-3333-111111111111");
    private static readonly Guid _task2Id = Guid.Parse("33333333-3333-3333-3333-222222222222");
    private static readonly Guid _task3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid _comment1Id = Guid.Parse("44444444-4444-4444-4444-111111111111");
    private static readonly Guid _checklist1Id = Guid.Parse("55555555-5555-5555-5555-111111111111");
    private static readonly Guid _checklist2Id = Guid.Parse("55555555-5555-5555-5555-222222222222");

    private static readonly List<TaskItemDto> _tasks =
    [
        new() { Id = _task1Id, Title = "Build dashboard UI", Description = "Create the main dashboard page",
                Priority = "High", Status = "InProgress", Features = "None",
                CategoryId = _cat1Id, CategoryName = "Development",
                StartDate = DateTimeOffset.UtcNow.AddDays(-5), DueDate = DateTimeOffset.UtcNow.AddDays(2),
                Tags = [new() { Id = _tag1Id, Name = "frontend", Color = "#3B82F6" }],
                ChecklistItems =
                [
                    new() { Id = _checklist1Id, Title = "Design mockups", IsCompleted = true, SortOrder = 1, TaskItemId = _task1Id },
                    new() { Id = _checklist2Id, Title = "Implement XAML", IsCompleted = false, SortOrder = 2, TaskItemId = _task1Id }
                ],
                Comments = [new() { Id = _comment1Id, Body = "Looking good so far!", TaskItemId = _task1Id }]
        },
        new() { Id = _task2Id, Title = "Fix login validation", Description = "Users can submit empty passwords",
                Priority = "Critical", Status = "Open", Features = "None",
                CategoryId = _cat1Id, CategoryName = "Development",
                DueDate = DateTimeOffset.UtcNow.AddDays(-1) },
        new() { Id = _task3Id, Title = "Write documentation", Description = "Update API docs for v2",
                Priority = "Low", Status = "Completed", Features = "None",
                CategoryId = _cat2Id, CategoryName = "Documentation",
                CompletedDate = DateTimeOffset.UtcNow.AddDays(-2) }
    ];

    private static readonly List<CategoryDto> _categories =
    [
        new() { Id = _cat1Id, Name = "Development", Description = "Dev tasks", SortOrder = 1, IsActive = true },
        new() { Id = _cat2Id, Name = "Documentation", Description = "Docs tasks", SortOrder = 2, IsActive = true, ParentCategoryId = null }
    ];

    private static readonly List<TagDto> _tags =
    [
        new() { Id = _tag1Id, Name = "frontend", Color = "#3B82F6" },
        new() { Id = _tag2Id, Name = "backend", Color = "#10B981" }
    ];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var path = request.RequestUri?.PathAndQuery ?? "";
        var method = request.Method;

        HttpResponseMessage response = (path, method.Method) switch
        {
            (var p, "POST") when p.Contains("/task-items/search") => JsonResponse(new PagedResponse<TaskItemDto>
                { Items = _tasks, TotalCount = _tasks.Count, PageNumber = 1, PageSize = 50 }),
            (var p, "GET") when p.Contains("/task-items/") => JsonResponse(_tasks.FirstOrDefault(t => p.Contains(t.Id.ToString()!))),
            (var p, "POST") when p.Contains("/task-items") => JsonResponse(_tasks[0]),
            (var p, "PUT") when p.Contains("/task-items/") => JsonResponse(_tasks[0]),
            (var p, "DELETE") when p.Contains("/task-items/") => new HttpResponseMessage(HttpStatusCode.NoContent),

            (var p, "POST") when p.Contains("/categories/search") => JsonResponse(new PagedResponse<CategoryDto>
                { Items = _categories, TotalCount = _categories.Count, PageNumber = 1, PageSize = 100 }),
            (var p, "GET") when p.Contains("/categories/") => JsonResponse(_categories[0]),
            (var p, "POST") when p.Contains("/categories") => JsonResponse(_categories[0]),
            (var p, "PUT") when p.Contains("/categories/") => JsonResponse(_categories[0]),
            (var p, "DELETE") when p.Contains("/categories/") => new HttpResponseMessage(HttpStatusCode.NoContent),

            (var p, "POST") when p.Contains("/tags/search") => JsonResponse(new PagedResponse<TagDto>
                { Items = _tags, TotalCount = _tags.Count, PageNumber = 1, PageSize = 100 }),
            (var p, "GET") when p.Contains("/tags/") => JsonResponse(_tags[0]),
            (var p, "POST") when p.Contains("/tags") => JsonResponse(_tags[0]),
            (var p, "PUT") when p.Contains("/tags/") => JsonResponse(_tags[0]),
            (var p, "DELETE") when p.Contains("/tags/") => new HttpResponseMessage(HttpStatusCode.NoContent),

            (var p, "POST") when p.Contains("/comments/search") => JsonResponse(new PagedResponse<CommentDto>
                { Items = _tasks[0].Comments ?? [], TotalCount = 1, PageNumber = 1, PageSize = 100 }),
            (var p, "POST") when p.Contains("/comments") => JsonResponse(_tasks[0].Comments?[0]),
            (var p, "PUT") when p.Contains("/comments/") => JsonResponse(_tasks[0].Comments?[0]),
            (var p, "DELETE") when p.Contains("/comments/") => new HttpResponseMessage(HttpStatusCode.NoContent),

            (var p, "POST") when p.Contains("/checklist-items/search") => JsonResponse(new PagedResponse<ChecklistItemDto>
                { Items = _tasks[0].ChecklistItems ?? [], TotalCount = 2, PageNumber = 1, PageSize = 100 }),
            (var p, "POST") when p.Contains("/checklist-items") => JsonResponse(_tasks[0].ChecklistItems?[0]),
            (var p, "PUT") when p.Contains("/checklist-items/") => JsonResponse(_tasks[0].ChecklistItems?[0]),
            (var p, "DELETE") when p.Contains("/checklist-items/") => new HttpResponseMessage(HttpStatusCode.NoContent),

            (var p, "POST") when p.Contains("/attachments/search") => JsonResponse(new PagedResponse<AttachmentDto>
                { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 100 }),
            (var p, "DELETE") when p.Contains("/attachments/") => new HttpResponseMessage(HttpStatusCode.NoContent),

            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };

        return Task.FromResult(response);
    }

    private static HttpResponseMessage JsonResponse<T>(T? data)
    {
        if (data is null)
            return new HttpResponseMessage(HttpStatusCode.NotFound);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(data, options: new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };
    }
}

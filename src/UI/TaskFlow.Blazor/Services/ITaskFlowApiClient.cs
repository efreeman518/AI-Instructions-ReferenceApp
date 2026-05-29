using EF.Common.Contracts;
using Refit;
using TaskFlow.Application.Models;

namespace TaskFlow.Blazor.Services;

/// <summary>
/// Refit client for the gateway-hosted TaskFlow API. It intentionally uses shared application
/// DTOs so Blazor exercises the same wire contract as endpoint tests and the other UI hosts.
/// </summary>
public interface ITaskFlowApiClient
{
    // ---- TaskItems ----
    [Post("/api/v1/task-items/search")]
    Task<PagedResponse<TaskItemDto>> SearchTaskItemsAsync(
        [Body] SearchRequest<TaskItemSearchFilter> request,
        CancellationToken ct = default);

    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    [Get("/api/v1/task-items/{id}")]
    Task<DefaultResponse<TaskItemDto>> GetTaskItemAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    [Post("/api/v1/task-items")]
    Task<DefaultResponse<TaskItemDto>> CreateTaskItemAsync(
        [Body] DefaultRequest<TaskItemDto> request,
        CancellationToken ct = default);

    /// <summary>Updates existing data after validation and preserves domain invariants.</summary>
    [Put("/api/v1/task-items/{id}")]
    Task<DefaultResponse<TaskItemDto>> UpdateTaskItemAsync(
        Guid id,
        [Body] DefaultRequest<TaskItemDto> request,
        CancellationToken ct = default);

    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    [Delete("/api/v1/task-items/{id}")]
    Task DeleteTaskItemAsync(Guid id, CancellationToken ct = default);

    // ---- Categories ----
    [Post("/api/v1/categories/search")]
    Task<PagedResponse<CategoryDto>> SearchCategoriesAsync(
        [Body] SearchRequest<CategorySearchFilter> request,
        CancellationToken ct = default);

    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    [Get("/api/v1/categories/{id}")]
    Task<DefaultResponse<CategoryDto>> GetCategoryAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    [Post("/api/v1/categories")]
    Task<DefaultResponse<CategoryDto>> CreateCategoryAsync(
        [Body] DefaultRequest<CategoryDto> request,
        CancellationToken ct = default);

    /// <summary>Updates existing data after validation and preserves domain invariants.</summary>
    [Put("/api/v1/categories/{id}")]
    Task<DefaultResponse<CategoryDto>> UpdateCategoryAsync(
        Guid id,
        [Body] DefaultRequest<CategoryDto> request,
        CancellationToken ct = default);

    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    [Delete("/api/v1/categories/{id}")]
    Task DeleteCategoryAsync(Guid id, CancellationToken ct = default);

    // ---- Tags ----
    [Post("/api/v1/tags/search")]
    Task<PagedResponse<TagDto>> SearchTagsAsync(
        [Body] SearchRequest<TagSearchFilter> request,
        CancellationToken ct = default);

    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    [Get("/api/v1/tags/{id}")]
    Task<DefaultResponse<TagDto>> GetTagAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    [Post("/api/v1/tags")]
    Task<DefaultResponse<TagDto>> CreateTagAsync(
        [Body] DefaultRequest<TagDto> request,
        CancellationToken ct = default);

    /// <summary>Updates existing data after validation and preserves domain invariants.</summary>
    [Put("/api/v1/tags/{id}")]
    Task<DefaultResponse<TagDto>> UpdateTagAsync(
        Guid id,
        [Body] DefaultRequest<TagDto> request,
        CancellationToken ct = default);

    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    [Delete("/api/v1/tags/{id}")]
    Task DeleteTagAsync(Guid id, CancellationToken ct = default);

    // ---- Comments ----
    [Post("/api/v1/comments/search")]
    Task<PagedResponse<CommentDto>> SearchCommentsAsync(
        [Body] SearchRequest<CommentSearchFilter> request,
        CancellationToken ct = default);

    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    [Post("/api/v1/comments")]
    Task<DefaultResponse<CommentDto>> CreateCommentAsync(
        [Body] DefaultRequest<CommentDto> request,
        CancellationToken ct = default);

    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    [Delete("/api/v1/comments/{id}")]
    Task DeleteCommentAsync(Guid id, CancellationToken ct = default);

    // ---- Checklist Items ----
    [Post("/api/v1/checklist-items/search")]
    Task<PagedResponse<ChecklistItemDto>> SearchChecklistItemsAsync(
        [Body] SearchRequest<ChecklistItemSearchFilter> request,
        CancellationToken ct = default);

    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    [Post("/api/v1/checklist-items")]
    Task<DefaultResponse<ChecklistItemDto>> CreateChecklistItemAsync(
        [Body] DefaultRequest<ChecklistItemDto> request,
        CancellationToken ct = default);

    /// <summary>Updates existing data after validation and preserves domain invariants.</summary>
    [Put("/api/v1/checklist-items/{id}")]
    Task<DefaultResponse<ChecklistItemDto>> UpdateChecklistItemAsync(
        Guid id,
        [Body] DefaultRequest<ChecklistItemDto> request,
        CancellationToken ct = default);

    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    [Delete("/api/v1/checklist-items/{id}")]
    Task DeleteChecklistItemAsync(Guid id, CancellationToken ct = default);
}

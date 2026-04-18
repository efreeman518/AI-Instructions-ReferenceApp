using EF.Common.Contracts;
using Refit;
using TaskFlow.Application.Models;

namespace TaskFlow.Blazor.Services;

public interface ITaskFlowApiClient
{
    // ---- TaskItems ----
    [Post("/api/task-items/search")]
    Task<PagedResponse<TaskItemDto>> SearchTaskItemsAsync(
        [Body] SearchRequest<TaskItemSearchFilter> request,
        CancellationToken ct = default);

    [Get("/api/task-items/{id}")]
    Task<DefaultResponse<TaskItemDto>> GetTaskItemAsync(Guid id, CancellationToken ct = default);

    [Post("/api/task-items")]
    Task<DefaultResponse<TaskItemDto>> CreateTaskItemAsync(
        [Body] DefaultRequest<TaskItemDto> request,
        CancellationToken ct = default);

    [Put("/api/task-items/{id}")]
    Task<DefaultResponse<TaskItemDto>> UpdateTaskItemAsync(
        Guid id,
        [Body] DefaultRequest<TaskItemDto> request,
        CancellationToken ct = default);

    [Delete("/api/task-items/{id}")]
    Task DeleteTaskItemAsync(Guid id, CancellationToken ct = default);

    // ---- Categories ----
    [Post("/api/categories/search")]
    Task<PagedResponse<CategoryDto>> SearchCategoriesAsync(
        [Body] SearchRequest<CategorySearchFilter> request,
        CancellationToken ct = default);

    [Get("/api/categories/{id}")]
    Task<DefaultResponse<CategoryDto>> GetCategoryAsync(Guid id, CancellationToken ct = default);

    [Post("/api/categories")]
    Task<DefaultResponse<CategoryDto>> CreateCategoryAsync(
        [Body] DefaultRequest<CategoryDto> request,
        CancellationToken ct = default);

    [Put("/api/categories/{id}")]
    Task<DefaultResponse<CategoryDto>> UpdateCategoryAsync(
        Guid id,
        [Body] DefaultRequest<CategoryDto> request,
        CancellationToken ct = default);

    [Delete("/api/categories/{id}")]
    Task DeleteCategoryAsync(Guid id, CancellationToken ct = default);

    // ---- Tags ----
    [Post("/api/tags/search")]
    Task<PagedResponse<TagDto>> SearchTagsAsync(
        [Body] SearchRequest<TagSearchFilter> request,
        CancellationToken ct = default);

    [Get("/api/tags/{id}")]
    Task<DefaultResponse<TagDto>> GetTagAsync(Guid id, CancellationToken ct = default);

    [Post("/api/tags")]
    Task<DefaultResponse<TagDto>> CreateTagAsync(
        [Body] DefaultRequest<TagDto> request,
        CancellationToken ct = default);

    [Put("/api/tags/{id}")]
    Task<DefaultResponse<TagDto>> UpdateTagAsync(
        Guid id,
        [Body] DefaultRequest<TagDto> request,
        CancellationToken ct = default);

    [Delete("/api/tags/{id}")]
    Task DeleteTagAsync(Guid id, CancellationToken ct = default);

    // ---- Comments ----
    [Post("/api/comments/search")]
    Task<PagedResponse<CommentDto>> SearchCommentsAsync(
        [Body] SearchRequest<CommentSearchFilter> request,
        CancellationToken ct = default);

    [Post("/api/comments")]
    Task<DefaultResponse<CommentDto>> CreateCommentAsync(
        [Body] DefaultRequest<CommentDto> request,
        CancellationToken ct = default);

    [Delete("/api/comments/{id}")]
    Task DeleteCommentAsync(Guid id, CancellationToken ct = default);

    // ---- Checklist Items ----
    [Post("/api/checklist-items/search")]
    Task<PagedResponse<ChecklistItemDto>> SearchChecklistItemsAsync(
        [Body] SearchRequest<ChecklistItemSearchFilter> request,
        CancellationToken ct = default);

    [Post("/api/checklist-items")]
    Task<DefaultResponse<ChecklistItemDto>> CreateChecklistItemAsync(
        [Body] DefaultRequest<ChecklistItemDto> request,
        CancellationToken ct = default);

    [Put("/api/checklist-items/{id}")]
    Task<DefaultResponse<ChecklistItemDto>> UpdateChecklistItemAsync(
        Guid id,
        [Body] DefaultRequest<ChecklistItemDto> request,
        CancellationToken ct = default);

    [Delete("/api/checklist-items/{id}")]
    Task DeleteChecklistItemAsync(Guid id, CancellationToken ct = default);
}

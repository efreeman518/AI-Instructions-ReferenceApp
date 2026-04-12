// Kiota client stub — replace with Kiota-generated client when OpenAPI spec is available.
// This stub provides the typed navigation structure matching the API surface.

namespace TaskFlow.Uno.Core.Client;

public class TaskFlowApiClient
{
    private readonly HttpClient _httpClient;

    public TaskFlowApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public ApiRequestBuilder Api => new(_httpClient);
}

public class ApiRequestBuilder
{
    private readonly HttpClient _http;
    public ApiRequestBuilder(HttpClient http) => _http = http;

    public TaskItemsRequestBuilder TaskItems => new(_http);
    public CategoriesRequestBuilder Categories => new(_http);
    public TagsRequestBuilder Tags => new(_http);
    public CommentsRequestBuilder Comments => new(_http);
    public ChecklistItemsRequestBuilder ChecklistItems => new(_http);
    public AttachmentsRequestBuilder Attachments => new(_http);
}

#region DTOs (transport layer — matches API contract)

public class TaskItemDto
{
    public Guid? Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public string? Features { get; set; }
    public decimal? EstimatedEffort { get; set; }
    public decimal? ActualEffort { get; set; }
    public DateTimeOffset? CompletedDate { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ParentTaskItemId { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public int? RecurrenceInterval { get; set; }
    public string? RecurrenceFrequency { get; set; }
    public DateTimeOffset? RecurrenceEndDate { get; set; }
    public string? CategoryName { get; set; }
    public List<CommentDto>? Comments { get; set; }
    public List<ChecklistItemDto>? ChecklistItems { get; set; }
    public List<TagDto>? Tags { get; set; }
    public List<TaskItemDto>? SubTasks { get; set; }
}

public class CategoryDto
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public Guid? ParentCategoryId { get; set; }
}

public class TagDto
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
}

public class CommentDto
{
    public Guid? Id { get; set; }
    public string? Body { get; set; }
    public Guid? TaskItemId { get; set; }
    public List<AttachmentDto>? Attachments { get; set; }
}

public class ChecklistItemDto
{
    public Guid? Id { get; set; }
    public string? Title { get; set; }
    public bool? IsCompleted { get; set; }
    public int? SortOrder { get; set; }
    public DateTimeOffset? CompletedDate { get; set; }
    public Guid? TaskItemId { get; set; }
}

public class AttachmentDto
{
    public Guid? Id { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? StorageUri { get; set; }
    public string? OwnerType { get; set; }
    public Guid? OwnerId { get; set; }
}

public class SearchRequest<TFilter> where TFilter : class, new()
{
    public TFilter Filter { get; set; } = new();
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class PagedResponse<T>
{
    public List<T>? Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class TaskItemSearchFilter
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public Guid? CategoryId { get; set; }
}

public class CategorySearchFilter
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public Guid? ParentCategoryId { get; set; }
}

public class TagSearchFilter
{
    public string? SearchTerm { get; set; }
}

public class CommentSearchFilter
{
    public Guid? TaskItemId { get; set; }
}

public class ChecklistItemSearchFilter
{
    public Guid? TaskItemId { get; set; }
    public bool? IsCompleted { get; set; }
}

public class AttachmentSearchFilter
{
    public Guid? OwnerId { get; set; }
    public string? OwnerType { get; set; }
}

#endregion

#region Request Builders

public class TaskItemsRequestBuilder
{
    private readonly HttpClient _http;
    public TaskItemsRequestBuilder(HttpClient http) => _http = http;

    public TaskItemsSearchRequestBuilder Search => new(_http);
    public TaskItemByIdRequestBuilder this[Guid id] => new(_http, id);

    public async Task<TaskItemDto?> PostAsync(TaskItemDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/task-items", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskItemDto>(cancellationToken);
    }
}

public class TaskItemsSearchRequestBuilder
{
    private readonly HttpClient _http;
    public TaskItemsSearchRequestBuilder(HttpClient http) => _http = http;

    public async Task<PagedResponse<TaskItemDto>?> PostAsync(SearchRequest<TaskItemSearchFilter> request,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/task-items/search", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResponse<TaskItemDto>>(cancellationToken);
    }
}

public class TaskItemByIdRequestBuilder
{
    private readonly HttpClient _http;
    private readonly Guid _id;
    public TaskItemByIdRequestBuilder(HttpClient http, Guid id) { _http = http; _id = id; }

    public async Task<TaskItemDto?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<TaskItemDto>($"/api/task-items/{_id}", cancellationToken);
    }

    public async Task<TaskItemDto?> PutAsync(TaskItemDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"/api/task-items/{_id}", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskItemDto>(cancellationToken);
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"/api/task-items/{_id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public class CategoriesRequestBuilder
{
    private readonly HttpClient _http;
    public CategoriesRequestBuilder(HttpClient http) => _http = http;

    public CategoriesSearchRequestBuilder Search => new(_http);
    public CategoryByIdRequestBuilder this[Guid id] => new(_http, id);

    public async Task<CategoryDto?> PostAsync(CategoryDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/categories", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken);
    }
}

public class CategoriesSearchRequestBuilder
{
    private readonly HttpClient _http;
    public CategoriesSearchRequestBuilder(HttpClient http) => _http = http;

    public async Task<PagedResponse<CategoryDto>?> PostAsync(SearchRequest<CategorySearchFilter> request,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/categories/search", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResponse<CategoryDto>>(cancellationToken);
    }
}

public class CategoryByIdRequestBuilder
{
    private readonly HttpClient _http;
    private readonly Guid _id;
    public CategoryByIdRequestBuilder(HttpClient http, Guid id) { _http = http; _id = id; }

    public async Task<CategoryDto?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<CategoryDto>($"/api/categories/{_id}", cancellationToken);
    }

    public async Task<CategoryDto?> PutAsync(CategoryDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"/api/categories/{_id}", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken);
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"/api/categories/{_id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public class TagsRequestBuilder
{
    private readonly HttpClient _http;
    public TagsRequestBuilder(HttpClient http) => _http = http;

    public TagsSearchRequestBuilder Search => new(_http);
    public TagByIdRequestBuilder this[Guid id] => new(_http, id);

    public async Task<TagDto?> PostAsync(TagDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/tags", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TagDto>(cancellationToken);
    }
}

public class TagsSearchRequestBuilder
{
    private readonly HttpClient _http;
    public TagsSearchRequestBuilder(HttpClient http) => _http = http;

    public async Task<PagedResponse<TagDto>?> PostAsync(SearchRequest<TagSearchFilter> request,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/tags/search", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResponse<TagDto>>(cancellationToken);
    }
}

public class TagByIdRequestBuilder
{
    private readonly HttpClient _http;
    private readonly Guid _id;
    public TagByIdRequestBuilder(HttpClient http, Guid id) { _http = http; _id = id; }

    public async Task<TagDto?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<TagDto>($"/api/tags/{_id}", cancellationToken);
    }

    public async Task<TagDto?> PutAsync(TagDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"/api/tags/{_id}", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TagDto>(cancellationToken);
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"/api/tags/{_id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public class CommentsRequestBuilder
{
    private readonly HttpClient _http;
    public CommentsRequestBuilder(HttpClient http) => _http = http;

    public CommentsSearchRequestBuilder Search => new(_http);
    public CommentByIdRequestBuilder this[Guid id] => new(_http, id);

    public async Task<CommentDto?> PostAsync(CommentDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/comments", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommentDto>(cancellationToken);
    }
}

public class CommentsSearchRequestBuilder
{
    private readonly HttpClient _http;
    public CommentsSearchRequestBuilder(HttpClient http) => _http = http;

    public async Task<PagedResponse<CommentDto>?> PostAsync(SearchRequest<CommentSearchFilter> request,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/comments/search", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResponse<CommentDto>>(cancellationToken);
    }
}

public class CommentByIdRequestBuilder
{
    private readonly HttpClient _http;
    private readonly Guid _id;
    public CommentByIdRequestBuilder(HttpClient http, Guid id) { _http = http; _id = id; }

    public async Task<CommentDto?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<CommentDto>($"/api/comments/{_id}", cancellationToken);
    }

    public async Task<CommentDto?> PutAsync(CommentDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"/api/comments/{_id}", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommentDto>(cancellationToken);
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"/api/comments/{_id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public class ChecklistItemsRequestBuilder
{
    private readonly HttpClient _http;
    public ChecklistItemsRequestBuilder(HttpClient http) => _http = http;

    public ChecklistItemsSearchRequestBuilder Search => new(_http);
    public ChecklistItemByIdRequestBuilder this[Guid id] => new(_http, id);

    public async Task<ChecklistItemDto?> PostAsync(ChecklistItemDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/checklist-items", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChecklistItemDto>(cancellationToken);
    }
}

public class ChecklistItemsSearchRequestBuilder
{
    private readonly HttpClient _http;
    public ChecklistItemsSearchRequestBuilder(HttpClient http) => _http = http;

    public async Task<PagedResponse<ChecklistItemDto>?> PostAsync(SearchRequest<ChecklistItemSearchFilter> request,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/checklist-items/search", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResponse<ChecklistItemDto>>(cancellationToken);
    }
}

public class ChecklistItemByIdRequestBuilder
{
    private readonly HttpClient _http;
    private readonly Guid _id;
    public ChecklistItemByIdRequestBuilder(HttpClient http, Guid id) { _http = http; _id = id; }

    public async Task<ChecklistItemDto?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<ChecklistItemDto>($"/api/checklist-items/{_id}", cancellationToken);
    }

    public async Task<ChecklistItemDto?> PutAsync(ChecklistItemDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"/api/checklist-items/{_id}", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChecklistItemDto>(cancellationToken);
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"/api/checklist-items/{_id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public class AttachmentsRequestBuilder
{
    private readonly HttpClient _http;
    public AttachmentsRequestBuilder(HttpClient http) => _http = http;

    public AttachmentsSearchRequestBuilder Search => new(_http);
    public AttachmentByIdRequestBuilder this[Guid id] => new(_http, id);

    public async Task<AttachmentDto?> PostAsync(AttachmentDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/attachments", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AttachmentDto>(cancellationToken);
    }
}

public class AttachmentsSearchRequestBuilder
{
    private readonly HttpClient _http;
    public AttachmentsSearchRequestBuilder(HttpClient http) => _http = http;

    public async Task<PagedResponse<AttachmentDto>?> PostAsync(SearchRequest<AttachmentSearchFilter> request,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/attachments/search", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResponse<AttachmentDto>>(cancellationToken);
    }
}

public class AttachmentByIdRequestBuilder
{
    private readonly HttpClient _http;
    private readonly Guid _id;
    public AttachmentByIdRequestBuilder(HttpClient http, Guid id) { _http = http; _id = id; }

    public async Task<AttachmentDto?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<AttachmentDto>($"/api/attachments/{_id}", cancellationToken);
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"/api/attachments/{_id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

#endregion

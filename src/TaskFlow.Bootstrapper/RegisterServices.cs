using EF.Common.Contracts;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Services;
using TaskFlow.Infrastructure.Repositories;

namespace TaskFlow.Bootstrapper;

public static class RegisterServices
{
    public static IServiceCollection AddTaskFlowServices(this IServiceCollection services)
    {
        // Request context — scaffold default (Phase 5f replaces with real auth-based context)
        services.AddScoped<IRequestContext<string, Guid?>>(sp =>
            new RequestContext<string, Guid?>("system", Guid.NewGuid().ToString(),
                Guid.Parse("00000000-0000-0000-0000-000000000001"), new List<string>()));

        // Cross-cutting
        services.AddScoped<ITenantBoundaryValidator, TenantBoundaryValidator>();
        services.AddSingleton<IEntityCacheProvider, NoOpEntityCacheProvider>();

        // Repositories
        services.AddScoped<ICategoryRepositoryTrxn, CategoryRepositoryTrxn>();
        services.AddScoped<ICategoryRepositoryQuery, CategoryRepositoryQuery>();
        services.AddScoped<ITagRepositoryTrxn, TagRepositoryTrxn>();
        services.AddScoped<ITagRepositoryQuery, TagRepositoryQuery>();
        services.AddScoped<ITaskItemRepositoryTrxn, TaskItemRepositoryTrxn>();
        services.AddScoped<ITaskItemRepositoryQuery, TaskItemRepositoryQuery>();
        services.AddScoped<ICommentRepositoryTrxn, CommentRepositoryTrxn>();
        services.AddScoped<ICommentRepositoryQuery, CommentRepositoryQuery>();
        services.AddScoped<IChecklistItemRepositoryTrxn, ChecklistItemRepositoryTrxn>();
        services.AddScoped<IChecklistItemRepositoryQuery, ChecklistItemRepositoryQuery>();
        services.AddScoped<IAttachmentRepositoryTrxn, AttachmentRepositoryTrxn>();
        services.AddScoped<IAttachmentRepositoryQuery, AttachmentRepositoryQuery>();
        services.AddScoped<ITaskItemTagRepositoryTrxn, TaskItemTagRepositoryTrxn>();
        services.AddScoped<ITaskItemTagRepositoryQuery, TaskItemTagRepositoryQuery>();

        // Services
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ITaskItemService, TaskItemService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IChecklistItemService, ChecklistItemService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<ITaskItemTagService, TaskItemTagService>();

        return services;
    }
}

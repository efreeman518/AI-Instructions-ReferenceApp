using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Services.Stubs;

namespace TaskFlow.Bootstrapper;

public static class RegisterServices
{
    public static IServiceCollection AddTaskFlowServices(this IServiceCollection services)
    {
        // No-op service stubs — replaced with real implementations in Phase 5b
        services.AddScoped<ICategoryService, CategoryServiceStub>();
        services.AddScoped<ITagService, TagServiceStub>();
        services.AddScoped<ITaskItemService, TaskItemServiceStub>();
        services.AddScoped<ICommentService, CommentServiceStub>();
        services.AddScoped<IChecklistItemService, ChecklistItemServiceStub>();
        services.AddScoped<IAttachmentService, AttachmentServiceStub>();
        services.AddScoped<ITaskItemTagService, TaskItemTagServiceStub>();

        return services;
    }
}

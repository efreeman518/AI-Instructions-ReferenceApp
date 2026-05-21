using EF.BackgroundServices.InternalMessageBus;
using EF.Common.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Cqrs.Registration;
using TaskFlow.Application.MessageHandlers;
using TaskFlow.Application.Services;

namespace TaskFlow.Bootstrapper;

public static partial class RegisterServices
{
    private static void AddApplicationServices(IServiceCollection services, IConfiguration config)
    {
        AddMessageHandlers(services);
        AddSharedApplicationServices(services);
        AddServiceApplicationServices(services);

        if (ApplicationStyleResolver.Resolve(config[ApplicationStyleResolver.ConfigKey]) == ApplicationStyle.Cqrs)
        {
            services.AddTaskFlowCqrsApplication();
        }

        services.AddScoped<ITaskViewProjectionService, TaskViewProjectionService>();
    }

    private static void AddSharedApplicationServices(IServiceCollection services)
    {
        services.AddScoped<ITenantBoundaryValidator, TenantBoundaryValidator>();
        services.AddSingleton<IEntityCacheProvider, NoOpEntityCacheProvider>();
    }

    private static void AddServiceApplicationServices(IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ITaskItemService, TaskItemService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IChecklistItemService, ChecklistItemService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<ITaskItemTagService, TaskItemTagService>();
    }

    private static void AddMessageHandlers(IServiceCollection services)
    {
        services.AddScoped<IMessageHandler<AuditEntry<string, Guid>>, AuditHandler>();
        services.AddScoped<IMessageHandler<AuditEntry<string, Guid?>>, AuditHandler>();
    }
}

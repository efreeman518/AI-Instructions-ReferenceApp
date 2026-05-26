using EF.CQRS.DependencyInjection;
using EF.CQRS.Validation;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Cqrs.Features.TaskItems;

namespace TaskFlow.Application.Cqrs.Registration;

public static class CqrsApplicationRegistration
{
    public static IServiceCollection AddTaskFlowCqrsApplication(this IServiceCollection services)
    {
        services.AddScoped<IRequestValidator<CreateTaskItemCommand>, CreateTaskItemCommandValidator>();
        services.AddScoped<IRequestValidator<UpdateTaskItemCommand>, UpdateTaskItemCommandValidator>();

        foreach (var registration in CqrsHandlerRegistrationCatalog.Registrations)
        {
            services.AddDecoratedRequestHandler(
                registration.RequestType,
                registration.ResponseType,
                registration.HandlerType);
        }

        return services;
    }
}

using EF.Common.Contracts;
using EF.CQRS.Validation;
using TaskFlow.Application.Cqrs.Shared;

namespace TaskFlow.Application.Cqrs.Features.TaskItems;

/// <summary>Provides create task item command validator behavior for the Features Task Items layer.</summary>
internal sealed class CreateTaskItemCommandValidator(IRequestContext<string, Guid?> requestContext)
    : IRequestValidator<CreateTaskItemCommand>
{
    /// <summary>Validates validate rules and returns failures before work continues.</summary>
    public Task<RequestValidationResult> ValidateAsync(CreateTaskItemCommand request, CancellationToken ct = default)
    {
        var dto = request.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;
        var validation = TaskItemStructureValidator.ValidateCreate(dto);
        return Task.FromResult(CqrsHandlerSupport.ToValidationResult(validation));
    }
}

/// <summary>Provides update task item command validator behavior for the Features Task Items layer.</summary>
internal sealed class UpdateTaskItemCommandValidator(IRequestContext<string, Guid?> requestContext)
    : IRequestValidator<UpdateTaskItemCommand>
{
    /// <summary>Validates validate rules and returns failures before work continues.</summary>
    public Task<RequestValidationResult> ValidateAsync(UpdateTaskItemCommand request, CancellationToken ct = default)
    {
        var dto = request.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;
        var validation = TaskItemStructureValidator.ValidateUpdate(dto);
        return Task.FromResult(CqrsHandlerSupport.ToValidationResult(validation));
    }
}

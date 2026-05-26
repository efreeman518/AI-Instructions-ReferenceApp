using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Shared;
using EF.CQRS.Validation;

namespace TaskFlow.Application.Cqrs.Features.TaskItems;

internal sealed class CreateTaskItemCommandValidator(IRequestContext<string, Guid?> requestContext)
    : IRequestValidator<CreateTaskItemCommand>
{
    public Task<RequestValidationResult> ValidateAsync(CreateTaskItemCommand request, CancellationToken ct = default)
    {
        var dto = request.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;
        var validation = TaskItemStructureValidator.ValidateCreate(dto);
        return Task.FromResult(CqrsHandlerSupport.ToValidationResult(validation));
    }
}

internal sealed class UpdateTaskItemCommandValidator(IRequestContext<string, Guid?> requestContext)
    : IRequestValidator<UpdateTaskItemCommand>
{
    public Task<RequestValidationResult> ValidateAsync(UpdateTaskItemCommand request, CancellationToken ct = default)
    {
        var dto = request.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;
        var validation = TaskItemStructureValidator.ValidateUpdate(dto);
        return Task.FromResult(CqrsHandlerSupport.ToValidationResult(validation));
    }
}

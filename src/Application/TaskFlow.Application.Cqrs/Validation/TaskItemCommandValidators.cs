using EF.Common.Contracts;
using EF.CQRS.Validation;
using TaskFlow.Application.Cqrs.Requests;

namespace TaskFlow.Application.Cqrs.Validation;

internal sealed class CreateTaskItemCommandValidator(IRequestContext<string, Guid?> requestContext)
    : IRequestValidator<CreateTaskItemCommand>
{
    public Task<RequestValidationResult> ValidateAsync(CreateTaskItemCommand request, CancellationToken ct = default)
    {
        var dto = request.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;
        var validation = TaskItemStructureValidator.ValidateCreate(dto);
        return Task.FromResult(RequestValidationMapper.ToValidationResult(validation));
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
        return Task.FromResult(RequestValidationMapper.ToValidationResult(validation));
    }
}

internal static class RequestValidationMapper
{
    public static RequestValidationResult ToValidationResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return RequestValidationResult.Valid();
        if (result.Errors.Count > 0) return RequestValidationResult.Failure(result.Errors);
        return RequestValidationResult.Failure([result.ErrorMessage ?? "Validation failed."]);
    }
}

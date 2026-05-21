# EF.CQRS

Lightweight CQRS primitives for applications that want direct command/query handler injection while avoiding central request dispatchers, request buses, and generic `Send()` entrypoints. The goal is explicit endpoint-to-handler wiring and predictable DI registration.

## What Belongs Here

- `ICommand<TResponse>` and `IQuery<TResponse>` marker interfaces.
- `IRequestHandler<TRequest,TResponse>` use-case handler contract.
- `IRequestValidator<TRequest>` and `RequestValidationResult` for custom validation.
- Validation and logging decorators.
- `AddDecoratedRequestHandler<TRequest,TResponse,THandler>()` DI registration.
- Reflection-based validation failure mapping for response types with a public static `Failure(...)` method, such as `Result` / `Result<T>` shapes.

## Expected Usage

```csharp
public sealed record CreateTaskCommand(DefaultRequest<TaskDto> Request)
    : ICommand<Result<DefaultResponse<TaskDto>>>;

internal sealed class CreateTaskHandler
    : IRequestHandler<CreateTaskCommand, Result<DefaultResponse<TaskDto>>>
{
    public Task<Result<DefaultResponse<TaskDto>>> HandleAsync(
        CreateTaskCommand request,
        CancellationToken ct = default)
    {
        // use-case flow
    }
}

services.AddDecoratedRequestHandler<
    CreateTaskCommand,
    Result<DefaultResponse<TaskDto>>,
    CreateTaskHandler>();
```

Register validators only where they remove repeated handler checks:

```csharp
services.AddRequestValidator<CreateTaskCommand, CreateTaskCommandValidator>();
```

If the response type does not expose a supported static `Failure(...)` method, register an `IValidationFailureResponseFactory<TResponse>` for that response type.

## Non-Goals

- No central request dispatcher or request bus.
- No generic `Send()` entrypoint.
- No hidden runtime request routing; callers inject the exact handler they need.
- No repository abstractions.
- No endpoint framework.
- No FluentValidation or third-party validation package.

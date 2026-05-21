namespace EF.CQRS.DependencyInjection;

public sealed record RequestHandlerRegistration(
    Type RequestType,
    Type ResponseType,
    Type HandlerType);

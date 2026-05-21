namespace EF.CQRS.DependencyInjection;

public sealed class DecoratedRequestHandlerOptions
{
    public bool EnableValidation { get; set; } = true;

    public bool EnableLogging { get; set; } = true;
}

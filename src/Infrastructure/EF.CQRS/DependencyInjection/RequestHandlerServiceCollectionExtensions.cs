using EF.CQRS.Abstractions;
using EF.CQRS.Decorators;
using EF.CQRS.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace EF.CQRS.DependencyInjection;

public static class RequestHandlerServiceCollectionExtensions
{
    public static IServiceCollection AddRequestValidator<TRequest, TValidator>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TValidator : class, IRequestValidator<TRequest>
    {
        services.Add(new ServiceDescriptor(typeof(IRequestValidator<TRequest>), typeof(TValidator), lifetime));
        return services;
    }

    public static IServiceCollection AddRequestHandler<TRequest, TResponse, THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where THandler : class, IRequestHandler<TRequest, TResponse>
    {
        services.Add(new ServiceDescriptor(typeof(THandler), typeof(THandler), lifetime));
        services.Add(new ServiceDescriptor(
            typeof(IRequestHandler<TRequest, TResponse>),
            sp => sp.GetRequiredService<THandler>(),
            lifetime));
        return services;
    }

    public static IServiceCollection AddDecoratedRequestHandler<TRequest, TResponse, THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Action<DecoratedRequestHandlerOptions>? configure = null)
        where THandler : class, IRequestHandler<TRequest, TResponse>
    {
        var options = new DecoratedRequestHandlerOptions();
        configure?.Invoke(options);

        services.TryAddTransient(typeof(IValidationFailureResponseFactory<>), typeof(StaticFailureValidationResponseFactory<>));
        services.Add(new ServiceDescriptor(typeof(THandler), typeof(THandler), lifetime));
        services.Add(new ServiceDescriptor(
            typeof(IRequestHandler<TRequest, TResponse>),
            sp =>
            {
                IRequestHandler<TRequest, TResponse> pipeline = sp.GetRequiredService<THandler>();

                if (options.EnableValidation)
                {
                    pipeline = new ValidationRequestHandlerDecorator<TRequest, TResponse>(
                        pipeline,
                        sp.GetServices<IRequestValidator<TRequest>>(),
                        sp.GetRequiredService<IValidationFailureResponseFactory<TResponse>>(),
                        sp.GetRequiredService<ILogger<ValidationRequestHandlerDecorator<TRequest, TResponse>>>());
                }

                if (options.EnableLogging)
                {
                    pipeline = new LoggingRequestHandlerDecorator<TRequest, TResponse>(
                        pipeline,
                        sp.GetRequiredService<ILogger<LoggingRequestHandlerDecorator<TRequest, TResponse>>>());
                }

                return pipeline;
            },
            lifetime));

        return services;
    }

    public static IServiceCollection AddDecoratedRequestHandler(
        this IServiceCollection services,
        Type requestType,
        Type responseType,
        Type handlerType,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Action<DecoratedRequestHandlerOptions>? configure = null)
    {
        var contract = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        if (!contract.IsAssignableFrom(handlerType))
        {
            throw new InvalidOperationException($"{handlerType.FullName} must implement {contract.FullName}.");
        }

        var method = typeof(RequestHandlerServiceCollectionExtensions)
            .GetMethod(nameof(AddDecoratedRequestHandlerCore), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(requestType, responseType, handlerType);

        method.Invoke(null, [services, lifetime, configure]);
        return services;
    }

    public static IServiceCollection AddDecoratedRequestHandlers(
        this IServiceCollection services,
        IEnumerable<RequestHandlerRegistration> registrations,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Action<DecoratedRequestHandlerOptions>? configure = null)
    {
        foreach (var registration in registrations)
        {
            services.AddDecoratedRequestHandler(
                registration.RequestType,
                registration.ResponseType,
                registration.HandlerType,
                lifetime,
                configure);
        }

        return services;
    }

    private static IServiceCollection AddDecoratedRequestHandlerCore<TRequest, TResponse, THandler>(
        IServiceCollection services,
        ServiceLifetime lifetime,
        Action<DecoratedRequestHandlerOptions>? configure)
        where THandler : class, IRequestHandler<TRequest, TResponse>
    {
        return services.AddDecoratedRequestHandler<TRequest, TResponse, THandler>(lifetime, configure);
    }
}

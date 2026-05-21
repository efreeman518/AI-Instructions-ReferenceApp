using System.Reflection;

namespace EF.CQRS.Validation;

/// <summary>
/// Builds validation failure responses for result-like types exposing a public static Failure(...) method.
/// </summary>
public sealed class StaticFailureValidationResponseFactory<TResponse> : IValidationFailureResponseFactory<TResponse>
{
    private static readonly Func<IReadOnlyCollection<string>, TResponse> Factory = BuildFactory();

    public TResponse CreateFailure(IReadOnlyCollection<string> errors) => Factory(errors);

    private static Func<IReadOnlyCollection<string>, TResponse> BuildFactory()
    {
        var methods = typeof(TResponse)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == "Failure")
            .Where(method => method.GetParameters().Length == 1)
            .ToArray();

        var collectionMethod = FindMethod(methods, typeof(IEnumerable<string>))
            ?? FindMethod(methods, typeof(IReadOnlyCollection<string>))
            ?? FindMethod(methods, typeof(IReadOnlyList<string>))
            ?? FindMethod(methods, typeof(string[]))
            ?? FindMethod(methods, typeof(List<string>));

        if (collectionMethod is not null)
        {
            return errors => Invoke(collectionMethod, ConvertCollection(errors, collectionMethod.GetParameters()[0].ParameterType));
        }

        var stringMethod = FindMethod(methods, typeof(string));
        if (stringMethod is not null)
        {
            return errors => Invoke(stringMethod, string.Join("; ", errors));
        }

        throw new InvalidOperationException(
            $"Response type {typeof(TResponse).FullName} must expose public static Failure(string) or Failure(IEnumerable<string>), " +
            $"or register a custom {typeof(IValidationFailureResponseFactory<TResponse>).FullName}.");
    }

    private static MethodInfo? FindMethod(IEnumerable<MethodInfo> methods, Type parameterType) =>
        methods.FirstOrDefault(method =>
        {
            var actual = method.GetParameters()[0].ParameterType;
            return actual == parameterType || actual.IsAssignableFrom(parameterType);
        });

    private static object ConvertCollection(IReadOnlyCollection<string> errors, Type parameterType)
    {
        if (parameterType == typeof(string[])) return errors.ToArray();
        if (parameterType == typeof(List<string>)) return errors.ToList();
        return errors;
    }

    private static TResponse Invoke(MethodInfo method, object argument) =>
        (TResponse)method.Invoke(null, [argument])!;
}

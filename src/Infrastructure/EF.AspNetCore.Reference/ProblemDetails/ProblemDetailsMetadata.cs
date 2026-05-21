using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EF.AspNetCore.ProblemDetails;

public static class ProblemDetailsMetadata
{
    public static void ApplyRequestMetadata(Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);
        ArgumentNullException.ThrowIfNull(httpContext);

        problemDetails.Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}";
        problemDetails.Extensions.TryAdd("traceId", httpContext.TraceIdentifier);

        var activity = Activity.Current;
        if (!string.IsNullOrWhiteSpace(activity?.Id))
            problemDetails.Extensions.TryAdd("activityId", activity.Id);
    }
}

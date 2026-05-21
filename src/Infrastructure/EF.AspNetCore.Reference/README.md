# EF.AspNetCore.Reference

Temporary local project for reusable ASP.NET Core conventions extracted from the TaskFlow reference app.

The package ID and assembly name are temporarily `EF.AspNetCore.Reference` so this repo can also consume the existing `EF.AspNetCore` feed package. Namespaces remain `EF.AspNetCore.*`; move these types into the feed package when promoting the extraction.

## What Belongs Here

- Versioned API document metadata.
- API versioning + API Explorer registration.
- Per-version OpenAPI document registration with `ShouldInclude` filtering.
- Helpers for building version sets and mapping versioned route groups.
- Correlation ID middleware and header propagation registration.
- Basic security headers middleware.
- ProblemDetails trace/activity metadata helpers.

## Expected Usage

```csharp
var documents = new[]
{
    new ApiVersionDocument(new ApiVersion(1, 0), "v1")
};

services.AddEfVersionedOpenApi(options =>
{
    options.Title = "Orders API";
    options.Description = "Orders service API";
    options.Documents.AddRange(documents);
});

var versionSet = app.BuildApiVersionSet(documents);
var api = app.MapVersionedApiGroup("/api/v{apiVersion:apiVersion}", versionSet, documents[0].Version);
```

## Non-Goals

- No domain-specific endpoint classes.
- No authentication policy opinions.
- No Scalar dependency.
- No application-specific exception mappings.

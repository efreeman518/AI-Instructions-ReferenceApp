using System.Text.Json;
using System.Text.Json.Serialization;

namespace Test.Support;

/// <summary>
/// Shared JSON options for test-side serialization, mirroring the API host's
/// ConfigureHttpJsonOptions (web-style casing + string enums). Centralized so
/// per-test options cannot drift and mask contract regressions.
/// </summary>
public static class JsonTestOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}

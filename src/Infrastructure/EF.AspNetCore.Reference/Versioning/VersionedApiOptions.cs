namespace EF.AspNetCore.Versioning;

public sealed class VersionedApiOptions
{
    public string Title { get; set; } = "API";
    public string Description { get; set; } = "";
    public string ApiExplorerGroupNameFormat { get; set; } = "'v'VVV";
    public bool EnableOpenApi { get; set; } = true;
    public bool AssumeDefaultVersionWhenUnspecified { get; set; }
    public List<ApiVersionDocument> Documents { get; } = [];

    public ApiVersionDocument DefaultDocument =>
        Documents.Count > 0
            ? Documents[0]
            : throw new InvalidOperationException("At least one API version document must be configured.");
}

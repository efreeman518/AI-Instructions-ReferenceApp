using System.Text.Json;
using System.Text.Json.Serialization;
using EF.FlowEngine.Abstractions;
using EF.FlowEngine.Definition;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskFlow.Bootstrapper;

namespace TaskFlow.Api.Workflows;

// Reads JSON workflow definitions from the Workflows/ directory at startup and
// registers them with IWorkflowRegistry. Definitions are saved as Active so they
// appear in the dashboard immediately. Re-runs are idempotent — Active definitions
// at the same version are skipped (registry returns a mutation exception we swallow).
public sealed class WorkflowSeedStartupTask(
    IWorkflowRegistry registry,
    IHostEnvironment env,
    ILogger<WorkflowSeedStartupTask> logger) : IStartupTask
{
    // JsonStringEnumConverter is required: workflow JSON uses string values like
    // `"status": "Active"` for DefinitionStatus and `"on": ["Match"]` for DecisionOutcome,
    // and without the converter System.Text.Json throws JsonException at deserialization.
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var folder = Path.Combine(env.ContentRootPath, "Workflows");
        if (!Directory.Exists(folder))
        {
            logger.LogInformation("Workflows directory not found at {Folder} — skipping seed", folder);
            return;
        }

        foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var def = JsonSerializer.Deserialize<WorkflowDefinition>(json, JsonOpts);
                if (def is null) continue;

                await registry.SaveAsync(def, ct);
                try
                {
                    await registry.TransitionStatusAsync(def.Id, def.Version, DefinitionStatus.Active, ct);
                }
                catch (Exception ex) when (ex is InvalidOperationException)
                {
                    // Already Active — fine.
                }
                logger.LogInformation("Seeded workflow {WorkflowId} v{Version} from {File}", def.Id, def.Version, Path.GetFileName(file));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to seed workflow from {File}", file);
            }
        }
    }
}

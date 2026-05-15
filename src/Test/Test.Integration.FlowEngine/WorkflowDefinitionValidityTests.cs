using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EF.FlowEngine.Definition;
using EF.FlowEngine.Impl;

namespace Test.Integration.FlowEngine;

// Tier 1+2: Validate each shipped workflow JSON deserializes, passes WorkflowDefinitionValidator,
// and round-trips through an in-memory IWorkflowRegistry (which re-runs the validator on SaveAsync).
//
// These tests catch most authoring mistakes that would otherwise only surface at first-instance-start
// in dev — unknown node types, dangling edge.nextNodeId references, missing entryNodeId, malformed
// outputSchema / paramsSchema JSON.
[TestClass]
public class WorkflowDefinitionValidityTests
{
    private const string TriageId = "ai-task-triage";
    private const string DecomposerId = "ai-task-decomposer";
    private const string ComplianceId = "compliance-check";

    public static IEnumerable<object[]> AllWorkflows() =>
    [
        ["ai-task-triage.json",      TriageId,      "1.0.0"],
        ["ai-task-decomposer.json",  DecomposerId,  "1.0.0"],
        ["compliance-check.json",    ComplianceId,  "1.0.0"],
    ];

    // Canonical options for FlowEngine workflow JSON — camelCase, string-named enums with
    // integer fallback. Both the seeding service and WorkflowDefinitionBuilder.FromJson use
    // this same instance; reusing it here keeps the test serializer in lock-step with runtime.
    private static readonly JsonSerializerOptions JsonOpts = WorkflowDefinitionJsonOptions.Default;

    [TestMethod]
    [DynamicData(nameof(AllWorkflows))]
    [TestCategory("Integration")]
    public void Each_Workflow_Json_Deserializes(string fileName, string expectedId, string expectedVersion)
    {
        var def = JsonSerializer.Deserialize<WorkflowDefinition>(ReadWorkflowFile(fileName), JsonOpts)!;

        Assert.AreEqual(expectedId, def.Id, "Workflow Id mismatch");
        Assert.AreEqual(expectedVersion, def.Version, "Workflow Version mismatch");
        Assert.IsFalse(string.IsNullOrWhiteSpace(def.EntryNodeId), "EntryNodeId missing");
        Assert.IsTrue(def.Nodes.ContainsKey(def.EntryNodeId), "EntryNodeId does not reference an existing node");
    }

    [TestMethod]
    [DynamicData(nameof(AllWorkflows))]
    [TestCategory("Integration")]
    public void Each_Workflow_Passes_DefinitionValidator(string fileName, string _id, string _version)
    {
        var def = JsonSerializer.Deserialize<WorkflowDefinition>(ReadWorkflowFile(fileName), JsonOpts)!;

        // ValidateAndThrow surfaces validator errors (unknown node types, dangling edges,
        // missing required fields, malformed JSON schemas) as a typed exception.
        WorkflowDefinitionValidator.ValidateAndThrow(def);
    }

    [TestMethod]
    [DynamicData(nameof(AllWorkflows))]
    [TestCategory("Integration")]
    public async Task Each_Workflow_Round_Trips_Through_InMemoryRegistry(string fileName, string id, string version)
    {
        var def = JsonSerializer.Deserialize<WorkflowDefinition>(ReadWorkflowFile(fileName), JsonOpts)!;

        var registry = new InMemoryWorkflowRegistry();
        await registry.SaveAsync(def);

        // Our JSON ships with status=Active, so the explicit transition is a no-op; mirror
        // the seeding service's idempotent pattern (swallow Active→Active) so the test is
        // robust if we ever flip the JSON to ship as Draft.
        try
        {
            await registry.TransitionStatusAsync(id, version, DefinitionStatus.Active);
        }
        catch (InvalidOperationException) { /* already Active */ }

        var loaded = await registry.GetAsync(id, version);
        Assert.IsNotNull(loaded, "Registry returned null for the version just saved");
        Assert.AreEqual(DefinitionStatus.Active, loaded!.Status, "Definition should be Active after save");
        Assert.AreEqual(def.Nodes.Count, loaded.Nodes.Count, "Node count drifted on round-trip");
    }

    [TestMethod]
    [DynamicData(nameof(AllWorkflows))]
    [TestCategory("Integration")]
    public void WorkflowDefinitionBuilder_FromJson_Round_Trips(string fileName, string expectedId, string expectedVersion)
    {
        // v1.0.104: WorkflowDefinitionBuilder.FromJson now uses WorkflowDefinitionJsonOptions.Default
        // and fails fast on shape mismatch. The blank-shell bug previously documented here is fixed.
        var def = WorkflowDefinitionBuilder.FromJson(ReadWorkflowFile(fileName)).Build();

        Assert.AreEqual(expectedId, def.Id, "Builder.FromJson should now hydrate Id");
        Assert.AreEqual(expectedVersion, def.Version, "Builder.FromJson should now hydrate Version");
        Assert.IsTrue(def.Nodes.Count > 0, "Builder.FromJson should now hydrate Nodes");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void All_Three_Workflows_Are_Present_In_Output()
    {
        // Guard against the copy-on-build glob in the csproj silently dropping files.
        var dir = Path.Combine(AppContext.BaseDirectory, "Workflows");
        Assert.IsTrue(Directory.Exists(dir), $"Workflows directory missing at {dir}");

        string[] expected = ["ai-task-triage.json", "ai-task-decomposer.json", "compliance-check.json"];
        foreach (var f in expected)
            Assert.IsTrue(File.Exists(Path.Combine(dir, f)), $"Missing workflow file: {f}");
    }

    private static string ReadWorkflowFile(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Workflows", fileName);
        return File.ReadAllText(path);
    }
}

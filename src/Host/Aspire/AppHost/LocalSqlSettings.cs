namespace AppHost;

/// <summary>
/// Hardcoded local-only credentials shared by the Aspire AppHost and Aspire-based tests.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why hardcoded?</b> This is a dev-loop-only credential consumed exclusively by the local SQL
/// Server container the AppHost spins up. It never reaches production — production services
/// (<c>TaskFlow.Api</c>, <c>TaskFlow.Functions</c>, etc.) receive connection strings from Azure
/// (Key Vault / Managed Identity); the AppHost project itself is not deployed.
/// </para>
/// <para>
/// <b>Why not <c>appsettings.json</c> / config?</b> Same outcome — the value still lives in source
/// control — but behind a configuration-key lookup. A typed constant is more discoverable, removes a
/// class of "wrong key" bugs, and lets both the AppHost and the test fixture import the same symbol
/// (one source of truth).
/// </para>
/// <para>
/// <b>Why not user-secrets?</b> Would force every developer, CI agent, and fresh-checkout test run to
/// execute <c>dotnet user-secrets set Parameters:sql-password ...</c> before <c>dotnet run</c> works.
/// We optimize for "git clone &amp;&amp; dotnet run" with zero side configuration.
/// </para>
/// <para>
/// <b>Why not environment variables?</b> Same first-run friction as user-secrets, plus tests would
/// need to save/restore <c>Parameters__sql-password</c> across the assembly lifetime — exactly the
/// pattern <c>AspireTestHost</c> recently moved away from in favor of <c>configureBuilder</c>
/// configuration injection.
/// </para>
/// <para>
/// <b>Why a stable value?</b> The AppHost configures SQL with
/// <c>WithDataVolume("taskflow-sql-data")</c> in non-test mode (see <c>AppHost.cs</c>). A named Docker
/// volume only remains usable while the SA password matches what the data files were initialized with;
/// randomizing per-developer would force a volume reset on every first run and break the "persistent
/// dev DB across restarts" experience.
/// </para>
/// <para>
/// <b>Override path.</b> The hardcoded value is only the <i>default</i> passed to
/// <c>builder.AddParameter("sql-password", defaultSqlPassword, secret: true)</c>. Aspire Parameter
/// resolution still honors the normal config chain first — env var <c>Parameters__sql-password</c>,
/// user-secret <c>Parameters:sql-password</c>, command-line <c>--Parameters:sql-password=...</c>, or
/// <c>hostSettings.Configuration["Parameters:sql-password"]</c> in tests via <c>configureBuilder</c>.
/// Anyone who wants to inject a different value can do so without editing this file.
/// </para>
/// <para>
/// <b>Threat model.</b> The "secret" guards a SQL container bound to <c>localhost</c> on a developer's
/// machine. Every realistic compromise scenario already requires pre-existing access to that machine,
/// at which point a dev DB password is not the smallest problem.
/// </para>
/// </remarks>
public static class LocalSqlSettings
{
    /// <summary>Shared local SA password for the Aspire AppHost SQL container and Aspire-based tests.</summary>
    public const string SharedSaPassword = "TaskFlow_Local_2026!";
}

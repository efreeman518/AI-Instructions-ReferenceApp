using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using System.ClientModel;

namespace TaskFlow.Bootstrapper;

/// <summary>Bootstraps Foundry Local and adapts its OpenAI-compatible endpoint to <see cref="IChatClient"/>.</summary>
internal static class FoundryLocalChatClient
{
    internal static async Task<IChatClient> CreateAsync(
        string modelAlias,
        string webUrl,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(modelAlias))
            throw new ArgumentException("Foundry Local model alias is required.", nameof(modelAlias));

        if (!Uri.TryCreate(webUrl, UriKind.Absolute, out var webUri))
            throw new ArgumentException("Foundry Local web URL must be absolute.", nameof(webUrl));

        var normalizedWebUrl = webUri.GetLeftPart(UriPartial.Authority);
        var config = new Configuration
        {
            AppName = "TaskFlow",
            LogLevel = Microsoft.AI.Foundry.Local.LogLevel.Information,
            Web = new Configuration.WebService { Urls = normalizedWebUrl }
        };

        if (!FoundryLocalManager.IsInitialized)
            await FoundryLocalManager.CreateAsync(config, logger, ct);

        var manager = FoundryLocalManager.Instance;
        await manager.DownloadAndRegisterEpsAsync((_, _) => { }, ct);

        var catalog = await manager.GetCatalogAsync(ct);
        var model = await catalog.GetModelAsync(modelAlias, ct)
            ?? throw new InvalidOperationException($"Foundry Local model '{modelAlias}' not found.");

        logger.LogInformation("Downloading Foundry Local model {ModelAlias} if needed.", modelAlias);
        await model.DownloadAsync(_ => { }, ct);

        logger.LogInformation("Loading Foundry Local model {ModelId}.", model.Id);
        await model.LoadAsync(ct);
        await manager.StartWebServiceAsync(ct);

        var endpoint = new Uri(normalizedWebUrl.TrimEnd('/') + "/v1");
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential("not-needed"),
            new OpenAIClientOptions { Endpoint = endpoint });

        return openAiClient.GetChatClient(model.Id).AsIChatClient();
    }
}

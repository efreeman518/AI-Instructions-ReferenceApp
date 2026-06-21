using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskFlow.Infrastructure.AI;

namespace TaskFlow.Bootstrapper;

public static partial class RegisterServices
{
    /// <summary>
    /// Registers the shared AI chat client before application services bind agents and demos.
    /// Azure Foundry wins when Aspire injects the chat connection; otherwise Foundry Local is
    /// attempted directly. Local startup failures fall through to the no-op client in AddAiServices.
    /// </summary>
    public static async Task RegisterAiChatClientAsync(
        this IHostApplicationBuilder builder,
        ILogger logger,
        CancellationToken ct = default)
    {
        var config = builder.Configuration;
        var appName = config.GetValue<string>("AppName") ?? builder.Environment.ApplicationName;
        var env = builder.Environment.EnvironmentName;

        var chatConnection = config.GetConnectionString("chat");
        if (!string.IsNullOrWhiteSpace(chatConnection))
        {
            logger.LogInformation("{AppName} {Environment} - Configure Azure AI Foundry chat client.", appName, env);
            builder.AddAzureChatCompletionsClient("chat")
                .AddChatClient();
            builder.Services.AddSingleton(new AiProviderInfo("azure"));
            return;
        }

        if (config.GetValue<bool>("AiServices:DisableFoundryLocal"))
            return;

        logger.LogInformation("{AppName} {Environment} - Configure Foundry Local chat client.", appName, env);

        try
        {
            var chatClient = await FoundryLocalChatClient.CreateAsync(
                config["AiServices:LocalModel"] ?? "qwen2.5-0.5b",
                config["AiServices:LocalWebUrl"] ?? "http://127.0.0.1:52415",
                logger,
                ct);
            builder.Services.AddSingleton<IChatClient>(chatClient);
            builder.Services.AddSingleton(new AiProviderInfo("local"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "{AppName} {Environment} - Foundry Local unavailable. Falling back to no-op AI client.",
                appName,
                env);
        }
    }
}

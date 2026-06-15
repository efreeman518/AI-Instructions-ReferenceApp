using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TaskFlow.Bootstrapper;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.AddServiceDefaults();

// AI chat client (Aspire-wired Foundry / Foundry Local) for the event-driven inference demo (D6).
// Registered only when the AppHost wired a "chat" deployment reference to this Functions host;
// otherwise AddAiServices registers a no-op IChatClient and the reviewer skips silently.
if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("chat")))
{
    builder.AddAzureChatCompletionsClient("chat").AddChatClient();
}

builder.Services
    .RegisterInfrastructureServices(builder.Configuration)
    .RegisterDomainServices(builder.Configuration)
    .RegisterApplicationServices(builder.Configuration)
    .RegisterBackgroundServices(builder.Configuration);

var app = builder.Build();
app.AutoRegisterMessageHandlers();
await app.RunAsync();

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskFlow.Bootstrapper;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.AddServiceDefaults();

var startupLogger = LoggerFactory
    .Create(logging => logging.AddConsole())
    .CreateLogger("TaskFlow.Functions");
await builder.RegisterAiChatClientAsync(startupLogger);

builder.Services
    .RegisterInfrastructureServices(builder.Configuration)
    .RegisterDomainServices(builder.Configuration)
    .RegisterApplicationServices(builder.Configuration)
    .RegisterBackgroundServices(builder.Configuration);

var app = builder.Build();
app.AutoRegisterMessageHandlers();
await app.RunAsync();

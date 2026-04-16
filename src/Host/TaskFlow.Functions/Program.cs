using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskFlow.Bootstrapper;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.AddServiceDefaults();
builder.Services
    .RegisterInfrastructureServices(builder.Configuration)
    .RegisterApplicationServices(builder.Configuration);

var app = builder.Build();
await app.RunAsync();

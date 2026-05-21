using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using TaskFlow.Gateway;

namespace Test.Unit.Gateway;

[TestClass]
public sealed class GatewayHealthCheckRegistrationTests
{
    [TestMethod]
    public void AddGatewayServices_WithServiceDefaults_ResolvesHealthCheckService()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["CorsSettings:AllowedOrigins:0"] = "https://localhost";

        builder.AddServiceDefaults();
        builder.Services.AddGatewayServices(builder.Configuration);

        using var provider = builder.Services.BuildServiceProvider();

        _ = provider.GetRequiredService<HealthCheckService>();
    }
}

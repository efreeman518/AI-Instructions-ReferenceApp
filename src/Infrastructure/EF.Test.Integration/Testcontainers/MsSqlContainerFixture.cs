using Testcontainers.MsSql;

namespace EF.Test.Integration.Testcontainers;

public sealed class MsSqlContainerFixture(string image = MsSqlContainerFixture.DefaultImage) : IAsyncDisposable
{
    public const string DefaultImage = "mcr.microsoft.com/mssql/server:2025-latest";

    private MsSqlContainer? _container;

    public bool IsStarted { get; private set; }

    public MsSqlContainer Container =>
        _container ?? throw new InvalidOperationException("SQL Server container has not been started.");

    public string ConnectionString =>
        IsStarted ? Container.GetConnectionString() : throw new InvalidOperationException("SQL Server container has not been started.");

    public async Task StartAsync()
    {
        if (IsStarted) return;

        _container = new MsSqlBuilder(image).Build();
        await _container.StartAsync();
        IsStarted = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();

        IsStarted = false;
    }
}

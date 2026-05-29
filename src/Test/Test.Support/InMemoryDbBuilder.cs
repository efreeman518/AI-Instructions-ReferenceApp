using Microsoft.EntityFrameworkCore;

namespace Test.Support;

/// <summary>Builds in memory DB test data with sensible defaults so tests only override relevant fields.</summary>
public class InMemoryDbBuilder
{
    private Action<DbContext>? _seedAction;

    /// <summary>Verifies use entity data behavior and protects the expected test contract.</summary>
    public InMemoryDbBuilder UseEntityData(Action<DbContext> seedAction)
    {
        _seedAction = seedAction;
        return this;
    }

    /// <summary>Builds in memory used by focused test cases.</summary>
    public T BuildInMemory<T>() where T : DbContext
    {
        var options = new DbContextOptionsBuilder<T>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = (T)Activator.CreateInstance(typeof(T), options)!;

        _seedAction?.Invoke(context);

        return context;
    }
}

using Microsoft.EntityFrameworkCore;

namespace Test.Support;

public class InMemoryDbBuilder
{
    private Action<DbContext>? _seedAction;

    public InMemoryDbBuilder UseEntityData(Action<DbContext> seedAction)
    {
        _seedAction = seedAction;
        return this;
    }

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

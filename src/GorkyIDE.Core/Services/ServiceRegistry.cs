using System.Collections.Concurrent;
using GorkyIDE.Abstractions.Services;

namespace GorkyIDE.Core.Services;

public sealed class ServiceRegistry : IServiceRegistry, IServiceProvider
{
    private readonly ConcurrentDictionary<Type, object> services = new();

    public void AddSingleton<TService>(TService instance)
        where TService : notnull
    {
        services[typeof(TService)] = instance;
    }

    public bool TryGet<TService>(out TService service)
        where TService : notnull
    {
        if (services.TryGetValue(typeof(TService), out var value) && value is TService typedValue)
        {
            service = typedValue;
            return true;
        }

        service = default!;
        return false;
    }

    public object? GetService(Type serviceType)
    {
        services.TryGetValue(serviceType, out var service);
        return service;
    }

    public IServiceProvider BuildServiceProvider() => this;
}

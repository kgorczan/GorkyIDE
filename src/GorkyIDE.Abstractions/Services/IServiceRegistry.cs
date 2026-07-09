namespace GorkyIDE.Abstractions.Services;

public interface IServiceRegistry
{
    void AddSingleton<TService>(TService instance)
        where TService : notnull;

    bool TryGet<TService>(out TService service)
        where TService : notnull;

    IServiceProvider BuildServiceProvider();
}

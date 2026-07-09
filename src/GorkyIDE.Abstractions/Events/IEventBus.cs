namespace GorkyIDE.Abstractions.Events;

public interface IEventBus
{
    ValueTask PublishAsync<TEvent>(TEvent ideEvent, CancellationToken cancellationToken)
        where TEvent : notnull;

    IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, ValueTask> handler)
        where TEvent : notnull;
}

using System.Collections.Concurrent;
using GorkyIDE.Abstractions.Events;

namespace GorkyIDE.Core.Events;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> handlersByType = new();

    public async ValueTask PublishAsync<TEvent>(TEvent ideEvent, CancellationToken cancellationToken)
        where TEvent : notnull
    {
        if (!handlersByType.TryGetValue(typeof(TEvent), out var handlers))
        {
            return;
        }

        Delegate[] snapshot;
        lock (handlers)
        {
            snapshot = handlers.ToArray();
        }

        foreach (var handler in snapshot.Cast<Func<TEvent, CancellationToken, ValueTask>>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await handler(ideEvent, cancellationToken).ConfigureAwait(false);
        }
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, ValueTask> handler)
        where TEvent : notnull
    {
        var handlers = handlersByType.GetOrAdd(typeof(TEvent), _ => new List<Delegate>());
        lock (handlers)
        {
            handlers.Add(handler);
        }

        return new Subscription(() =>
        {
            lock (handlers)
            {
                handlers.Remove(handler);
            }
        });
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action unsubscribe;
        private int disposed;

        public Subscription(Action unsubscribe)
        {
            this.unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                unsubscribe();
            }
        }
    }
}

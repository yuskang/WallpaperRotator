using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using WallpaperRotator.Core.Interfaces;

namespace WallpaperRotator.Infrastructure;

/// <summary>
/// 記憶體內事件總線實現
/// </summary>
public sealed class InMemoryEventBus : IEventBus
{
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _lock = new();

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
    {
        _logger = logger;
    }

    public void Publish<TEvent>(TEvent eventData) where TEvent : class
    {
        var eventType = typeof(TEvent);

        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            _logger.LogDebug("No handlers registered for event type: {EventType}", eventType.Name);
            return;
        }

        _logger.LogDebug("Publishing event {EventType} to {HandlerCount} handlers",
            eventType.Name, handlers.Count);

        foreach (var handler in handlers.ToList())
        {
            try
            {
                switch (handler)
                {
                    case Action<TEvent> syncHandler:
                        syncHandler(eventData);
                        break;
                    case Func<TEvent, Task> asyncHandler:
                        // Fire and forget for async handlers
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await asyncHandler(eventData);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error in async event handler for {EventType}",
                                    eventType.Name);
                            }
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in event handler for {EventType}", eventType.Name);
            }
        }
    }

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        return SubscribeInternal<TEvent>(handler);
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        return SubscribeInternal<TEvent>(handler);
    }

    private IDisposable SubscribeInternal<TEvent>(Delegate handler) where TEvent : class
    {
        var eventType = typeof(TEvent);

        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                _handlers[eventType] = handlers;
            }

            handlers.Add(handler);
        }

        _logger.LogDebug("Handler subscribed for event type: {EventType}", eventType.Name);

        return new Subscription(() =>
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(eventType, out var handlers))
                {
                    handlers.Remove(handler);
                    _logger.LogDebug("Handler unsubscribed from event type: {EventType}", eventType.Name);
                }
            }
        });
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action _unsubscribe;
        private bool _disposed;

        public Subscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _unsubscribe();
            _disposed = true;
        }
    }
}

namespace WallpaperRotator.Core.Interfaces;

/// <summary>
/// 事件總線介面
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 發布事件
    /// </summary>
    void Publish<TEvent>(TEvent eventData) where TEvent : class;

    /// <summary>
    /// 訂閱事件
    /// </summary>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;

    /// <summary>
    /// 訂閱事件 (異步處理)
    /// </summary>
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
}

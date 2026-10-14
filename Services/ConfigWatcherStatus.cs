#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Services;

/// <summary>
/// Tracks the state of the in-process configuration change watcher: whether its
/// event subscriptions are active, and when a change was last propagated to consumers.
/// Written by the host wiring in <c>Program.cs</c> and read by the configuration health check.
/// </summary>
public sealed class ConfigWatcherStatus
{
    private volatile bool _isRunning;
    private long _lastReloadTicks;

    /// <summary>
    /// Gets a value indicating whether the watcher's event subscriptions are active.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Gets the UTC time of the last change that was handled successfully, or <see langword="null"/> if none has been handled yet.
    /// </summary>
    public DateTime? LastReloadUtc
    {
        get
        {
            var ticks = Interlocked.Read(ref _lastReloadTicks);
            return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
        }
    }

    /// <summary>
    /// Marks the watcher as running.
    /// </summary>
    public void MarkStarted() => _isRunning = true;

    /// <summary>
    /// Marks the watcher as stopped.
    /// </summary>
    public void MarkStopped() => _isRunning = false;

    /// <summary>
    /// Records that a configuration change was handled successfully at the current time.
    /// </summary>
    public void RecordReload() => Interlocked.Exchange(ref _lastReloadTicks, DateTime.UtcNow.Ticks);
}

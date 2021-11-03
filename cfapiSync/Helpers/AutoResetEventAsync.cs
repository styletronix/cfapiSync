#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents an event that, when signaled, resets automatically after releasing a single waiting task.
/// </summary>
public sealed class AutoResetEventAsync : IDisposable
{

    /// <summary>
    /// Waits asynchronously until a signal is received.
    /// </summary>
    /// <returns>Task completed when the event is signaled.</returns>
    public async ValueTask WaitAsync()
    {
        if (CheckSignaled())
        {
            return;
        }

        SemaphoreSlim s;
        lock (Q)
        {
            Q.Enqueue(s = new(0, 1));
        }

        await s.WaitAsync();
        lock (Q)
        {
            if (Q.Count > 0 && Q.Peek() == s)
            {
                Q.Dequeue().Dispose();
            }
        }
    }

    /// <summary>
    /// Waits asynchronously until a signal is received or the time runs out.
    /// </summary>
    /// <param name="millisecondsTimeout">The number of milliseconds to wait, <see cref="System.Threading.Timeout.Infinite"/>
    /// (-1) to wait indefinitely, or zero to return immediately.</param>
    /// <returns>Task completed when the event is signaled or the time runs out.</returns>
    public async ValueTask WaitAsync(int millisecondsTimeout)
    {
        if (CheckSignaled())
        {
            return;
        }

        SemaphoreSlim s;
        lock (Q)
        {
            Q.Enqueue(s = new(0, 1));
        }

        await s.WaitAsync(millisecondsTimeout);
        lock (Q)
        {
            if (Q.Count > 0 && Q.Peek() == s)
            {
                Q.Dequeue().Dispose();
            }
        }
    }

    /// <summary>
    /// Waits asynchronously until a signal is received, the time runs out or the token is cancelled.
    /// </summary>
    /// <param name="millisecondsTimeout">The number of milliseconds to wait, <see cref="System.Threading.Timeout.Infinite"/>
    /// (-1) to wait indefinitely, or zero to return immediately.</param>
    /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> to observe.</param>
    /// <returns>Task completed when the event is signaled, the time runs out or the token is cancelled.</returns>
    public async ValueTask WaitAsync(int millisecondsTimeout, CancellationToken cancellationToken)
    {
        if (CheckSignaled())
        {
            return;
        }

        SemaphoreSlim s;
        lock (Q)
        {
            Q.Enqueue(s = new(0, 1));
        }

        try
        {
            await s.WaitAsync(millisecondsTimeout, cancellationToken);
        }
        finally
        {
            lock (Q)
            {
                if (Q.Count > 0 && Q.Peek() == s)
                {
                    Q.Dequeue().Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Waits asynchronously until a signal is received or the token is cancelled.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> to observe.</param>
    /// <returns>Task completed when the event is signaled or the token is cancelled.</returns>
    public async ValueTask WaitAsync(CancellationToken cancellationToken)
    {
        if (CheckSignaled())
        {
            return;
        }

        SemaphoreSlim s;
        lock (Q)
        {
            Q.Enqueue(s = new(0, 1));
        }

        try
        {
            await s.WaitAsync(cancellationToken);
        }
        finally
        {
            lock (Q)
            {
                if (Q.Count > 0 && Q.Peek() == s)
                {
                    Q.Dequeue().Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Waits asynchronously until a signal is received or the time runs out.
    /// </summary>
    /// <param name="timeout">A <see cref="System.TimeSpan"/> that represents the number of milliseconds to wait,
    /// a <see cref="System.TimeSpan"/> that represents -1 milliseconds to wait indefinitely, or a System.TimeSpan
    /// that represents 0 milliseconds to return immediately.</param>
    /// <returns>Task completed when the event is signaled or the time runs out.</returns>
    public async ValueTask WaitAsync(TimeSpan timeout)
    {
        if (CheckSignaled())
        {
            return;
        }

        SemaphoreSlim s;
        lock (Q)
        {
            Q.Enqueue(s = new(0, 1));
        }

        await s.WaitAsync(timeout);
        lock (Q)
        {
            if (Q.Count > 0 && Q.Peek() == s)
            {
                Q.Dequeue().Dispose();
            }
        }
    }

    /// <summary>
    /// Waits asynchronously until a signal is received, the time runs out or the token is cancelled.
    /// </summary>
    /// <param name="timeout">A <see cref="System.TimeSpan"/> that represents the number of milliseconds to wait,
    /// a <see cref="System.TimeSpan"/> that represents -1 milliseconds to wait indefinitely, or a System.TimeSpan
    /// that represents 0 milliseconds to return immediately.</param>
    /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> to observe.</param>
    /// <returns>Task completed when the event is signaled, the time runs out or the token is cancelled.</returns>
    public async ValueTask WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (CheckSignaled())
        {
            return;
        }

        SemaphoreSlim s;
        lock (Q)
        {
            Q.Enqueue(s = new(0, 1));
        }

        try
        {
            await s.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            lock (Q)
            {
                if (Q.Count > 0 && Q.Peek() == s)
                {
                    Q.Dequeue().Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Sets the state of the event to signaled, allowing one or more waiting tasks to proceed.
    /// </summary>
    public void Set()
    {
        SemaphoreSlim? toRelease = null;
        lock (Q)
        {
            if (Q.Count > 0)
            {
                toRelease = Q.Dequeue();
            }
            else if (!IsSignaled)
            {
                IsSignaled = true;
            }
        }
        toRelease?.Release();
    }

    /// <summary>
    /// Sets the state of the event to non nonsignaled, making the waiting tasks to wait.
    /// </summary>
    public void Reset()
    {
        IsSignaled = false;
    }

    /// <summary>
    /// Disposes any semaphores left in the queue.
    /// </summary>
    public void Dispose()
    {
        lock (Q)
        {
            while (Q.Count > 0)
            {
                Q.Dequeue().Dispose();
            }
        }
    }

    /// <summary>
    /// Checks the <see cref="IsSignaled"/> state and resets it when it's signaled.
    /// </summary>
    /// <returns>True if the event was in signaled state.</returns>
    private bool CheckSignaled()
    {
        lock (Q)
        {
            if (IsSignaled)
            {
                IsSignaled = false;
                return true;
            }
            return false;
        }
    }

    private readonly Queue<SemaphoreSlim> Q = new();
    private volatile bool IsSignaled;

}
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Hosting.Wpf.Internal;

internal sealed class DisposableList<T> : IDisposable
{
    private readonly List<IDisposable> _disposables = new();

    private bool _disposeCalled;

    private readonly object _lockObject = new();

    public void Add(T item)
    {
        CaptureDisposable(item);
    }

    private void CaptureDisposable(T item)
    {
        if (item is IDisposable disposable)
        {
            lock (_lockObject)
            {
                _disposables.Add(disposable);
            }
        }
    }

    /// <summary>
    /// Disposes every item in reverse order, also when some of them throw.
    /// </summary>
    /// <exception cref="AggregateException">One or more items threw; all items were still disposed.</exception>
    public void Dispose()
    {
        List<Exception>? failures = null;
        lock (_lockObject)
        {
            if (_disposeCalled)
            {
                return;
            }

            _disposeCalled = true;
            for (var i = _disposables.Count - 1; i >= 0; i--)
            {
                try
                {
                    _disposables[i].Dispose();
                }
#pragma warning disable CA1031 // Collected and rethrown after the remaining items are disposed.
                catch (Exception exception)
#pragma warning restore CA1031
                {
                    (failures ??= new List<Exception>()).Add(exception);
                }
            }

            _disposables.Clear();
        }

        if (failures is not null)
        {
            throw new AggregateException(failures);
        }
    }
}
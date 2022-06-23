using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Hosting.Wpf.Internal;

internal class DisposableList<T> : IDisposable
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

    public void Dispose()
    {
        lock (_lockObject)
        {
            if (_disposeCalled)
            {
                return;
            }

            _disposeCalled = true;
            for (var i = _disposables.Count - 1; i >= 0; i--)
            {
                var disposable = _disposables[i];
                disposable.Dispose();
            }

            _disposables.Clear();
        }
    }
}
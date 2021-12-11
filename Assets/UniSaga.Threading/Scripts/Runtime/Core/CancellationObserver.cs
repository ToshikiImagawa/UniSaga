// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Threading;

namespace UniSaga.Threading.Core
{
    internal class CancellationObserver<T> : IObserver<T>
    {
        private CancellationTokenSource _cancellationTokenSource;

        public CancellationObserver(CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;
        }

        public void OnCompleted()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }

        public void OnError(Exception error)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }

        public void OnNext(T value)
        {
        }
    }
}
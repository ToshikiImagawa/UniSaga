using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using UniRedux;
using UniSystem.Reactive.Disposables;

namespace UniSaga
{
    public delegate IEnumerator<IEffect> Saga();

    public delegate IEnumerator<IEffect> Saga<in TArgument>(
        TArgument argument
    );

    public delegate IEnumerator<IEffect> Saga<in TArgument1, in TArgument2>(
        TArgument1 argument1,
        TArgument2 argument2
    );

    public delegate IEnumerator<IEffect> Saga<in TArgument1, in TArgument2, in TArgument3>(
        TArgument1 argument1,
        TArgument2 argument2,
        TArgument3 argument3
    );

    public sealed class SagaTask
    {
        [CanBeNull] private CancellationTokenSource _cancellationTokenSource;
        [CanBeNull] private SagaTask _parentTask;
        private readonly List<SagaTask> _childTasks = new List<SagaTask>();
        private readonly object _lock = new object();
        private readonly ErrorObserver _errorObserver = new ErrorObserver();

        internal SagaTask(CancellationTokenSource cancellationTokenSource, SagaTask parentTask = null)
        {
            _cancellationTokenSource = cancellationTokenSource;
            _parentTask = parentTask;
            _parentTask?.SetChildTask(this);
        }

        public IObservable<Exception> Error => _errorObserver;
        public bool IsCompleted { get; private set; }
        internal CancellationToken Token => _cancellationTokenSource?.Token ?? CancellationToken.None;

        internal bool TryComplete()
        {
            if (IsCompleted) return true;
            SagaTask parentTask;
            lock (_lock)
            {
                if (_childTasks.Count != 0) return IsCompleted;
                parentTask = _parentTask;
                IsCompleted = true;
                _parentTask = null;
                _cancellationTokenSource = null;
            }

            parentTask?.RemoveChildTask(this);
            return IsCompleted;
        }

        internal void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        internal void SetError([NotNull] Exception error)
        {
            _errorObserver.OnNext(error);
        }

        private void SetChildTask(SagaTask childTask)
        {
            if (IsCompleted) return;
            lock (_lock)
            {
                if (_childTasks.Contains(childTask)) throw new InvalidOperationException();
                _childTasks.Add(childTask);
            }
        }

        private void RemoveChildTask(SagaTask childTask)
        {
            if (IsCompleted) return;
            lock (_lock)
            {
                _childTasks.Remove(childTask);
            }
        }

        private class ErrorObserver : IObservable<Exception>
        {
            private Exception _exception;
            private readonly object _lock = new object();
            private readonly List<IObserver<Exception>> _observers = new List<IObserver<Exception>>();

            public void OnNext([NotNull] Exception exception)
            {
                IObserver<Exception>[] observers;
                lock (_lock)
                {
                    if (_exception != null) return;
                    _exception = exception;
                    observers = _observers.ToArray();
                    _observers.Clear();
                }

                foreach (var observer in observers)
                {
                    observer.OnNext(_exception);
                    observer.OnCompleted();
                }
            }

            public IDisposable Subscribe(IObserver<Exception> observer)
            {
                lock (_lock)
                {
                    if (_exception == null)
                    {
                        _observers.Add(observer);
                        return Disposable.Create(() => { _observers.Remove(observer); });
                    }
                }

                observer.OnNext(_exception);
                observer.OnCompleted();
                return Disposable.Empty;
            }
        }
    }
}
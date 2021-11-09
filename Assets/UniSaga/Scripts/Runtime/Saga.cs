using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using UniSystem.Reactive.Disposables;
using UnityEngine;

namespace UniSaga
{
    public delegate IEnumerator Saga();

    public delegate IEnumerator Saga<in TArgument>(
        TArgument argument
    );

    public delegate IEnumerator Saga<in TArgument1, in TArgument2>(
        TArgument1 argument1,
        TArgument2 argument2
    );

    public delegate IEnumerator Saga<in TArgument1, in TArgument2, in TArgument3>(
        TArgument1 argument1,
        TArgument2 argument2,
        TArgument3 argument3
    );

    public sealed class SagaTask : IDisposable
    {
        [CanBeNull] private CancellationTokenSource _cancellationTokenSource;
        [NotNull] private readonly SagaTask _rootTask;
        private readonly List<SagaTask> _childTasks = new List<SagaTask>();
        private readonly object _lock = new object();
        private bool _disposed;
        private readonly ErrorObserver _errorObserver = new ErrorObserver();

        internal SagaTask(CancellationTokenSource cancellationTokenSource, SagaTask parentTask = null)
        {
            _cancellationTokenSource = cancellationTokenSource;
            IsRootSagaTask = parentTask == null;
            _rootTask = parentTask?._rootTask ?? this;
            if (parentTask == null) return;
            if (parentTask.IsError || parentTask.IsCanceled || parentTask.IsCompleted)
                throw new InvalidOperationException();
            parentTask._rootTask.SetChildTask(this);
        }

        [CanBeNull] internal Coroutine Coroutine { get; set; }
        public IObservable<Exception> Error => _errorObserver;
        public bool IsRootSagaTask { get; }
        public bool IsCompleted { get; private set; }
        public bool IsCanceled { get; private set; }
        public bool IsError { get; private set; }
        internal CancellationToken Token => _cancellationTokenSource?.Token ?? CancellationToken.None;

        internal bool TryComplete()
        {
            if (IsError) return true;
            if (IsCanceled) return true;
            if (IsCompleted) return true;
            SagaTask rootTask;
            lock (_lock)
            {
                if (_childTasks.Count != 0) return IsCompleted;
                rootTask = _rootTask;
                IsCompleted = true;
                _cancellationTokenSource = null;
            }

            if (rootTask != this) rootTask.RemoveChildTask(this);
            return IsCompleted;
        }

        internal void Cancel()
        {
            if (IsError) return;
            if (IsCompleted) return;
            IsCanceled = true;
            _cancellationTokenSource?.Cancel();
            if (Coroutine != null) UniSagaRunner.Instance.StopCoroutine(Coroutine);
            if (IsRootSagaTask)
            {
                SagaTask[] tasks;
                lock (_lock)
                {
                    tasks = _childTasks.ToArray();
                    _childTasks.Clear();
                }

                foreach (var task in tasks)
                {
                    task.Cancel();
                }
            }
            else
            {
                _rootTask.RemoveChildTask(this);
            }
        }

        internal void SetError([NotNull] Exception error)
        {
            if (error is OperationCanceledException)
            {
                if (IsCanceled) return;
                Cancel();
                return;
            }

            IsError = true;
            _errorObserver.OnNext(error);
            if (IsRootSagaTask)
            {
                SagaTask[] tasks;
                lock (_lock)
                {
                    tasks = _childTasks.ToArray();
                    _childTasks.Clear();
                }

                foreach (var task in tasks)
                {
                    task.SetError(error);
                }
            }
            else
            {
                _rootTask.RemoveChildTask(this);
            }
        }

        private void SetChildTask(SagaTask childTask)
        {
            lock (_lock)
            {
                if (_childTasks.Contains(childTask)) throw new InvalidOperationException();
                _childTasks.Add(childTask);
            }
        }

        private void RemoveChildTask(SagaTask childTask)
        {
            if (IsError) return;
            if (IsCanceled) return;
            if (IsCompleted) return;
            lock (_lock)
            {
                _childTasks.Remove(childTask);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (IsCompleted) return;
            if (IsError) return;
            if (IsCanceled) return;
            Cancel();
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
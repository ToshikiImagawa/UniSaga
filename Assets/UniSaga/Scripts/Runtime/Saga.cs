using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using UniSaga.Core;
using UniSaga.Plugin;
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

    public sealed class SagaCoroutine : IPlayerLoopItem
    {
        [NotNull] private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        [NotNull] private readonly List<SagaCoroutine> _childCoroutines = new List<SagaCoroutine>();
        [NotNull] private readonly SagaCoroutine _rootCoroutine;
        [NotNull] private readonly IEffectRunner _effectRunner;
        [NotNull] private readonly IEnumerator _enumerator;
        [NotNull] private readonly ErrorObserver _errorObserver = new ErrorObserver();
        private bool _requestCancel;

        private SagaCoroutine(
            [NotNull] IEffectRunner effectRunner,
            IEnumerator enumerator
        )
        {
            _rootCoroutine = this;
            _effectRunner = effectRunner;
            _enumerator = InnerEnumerator(enumerator);
        }

        private SagaCoroutine(
            [NotNull] SagaCoroutine sagaCoroutine,
            IEnumerator enumerator
        )
        {
            _effectRunner = sagaCoroutine._effectRunner;
            _enumerator = InnerEnumerator(enumerator);
            _rootCoroutine = sagaCoroutine._rootCoroutine;
            _rootCoroutine._childCoroutines.Add(this);
        }

        public bool IsError { get; private set; }
        public bool IsCanceled { get; private set; }
        public bool IsCompleted { get; private set; }

        public IObservable<Exception> Error => _errorObserver;
        internal CancellationToken Token => _cancellationTokenSource.Token;

        internal void RequestCancel()
        {
            _requestCancel = true;
        }

        internal void SetError(Exception error)
        {
            IsError = true;
            _errorObserver.OnNext(error);
        }

        bool IPlayerLoopItem.MoveNext()
        {
            if (IsCompleted || IsCanceled || IsError) return false;
            if (_requestCancel)
            {
                IsCanceled = true;
                _cancellationTokenSource.Cancel();
                return false;
            }

            try
            {
                if (_enumerator.MoveNext()) return true;
            }
            catch (Exception error)
            {
                SetError(error);
                return false;
            }

            IsCompleted = true;
            return false;
        }

        internal static SagaCoroutine StartCoroutine(
            [NotNull] IEffectRunner effectRunner,
            [NotNull] IEnumerator enumerator
        )
        {
            var coroutine = new SagaCoroutine(effectRunner, enumerator);
            PlayerLoopHelper.AddAction(coroutine);
            return coroutine;
        }

        internal SagaCoroutine StartCoroutine([NotNull] IEnumerator enumerator)
        {
            var coroutine = new SagaCoroutine(this, enumerator);
            PlayerLoopHelper.AddAction(coroutine);
            return coroutine;
        }

        private IEnumerator InnerEnumerator(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                switch (current)
                {
                    case null:
                        yield return null;
                        break;
                    case CustomYieldInstruction cyi:
                    {
                        while (cyi.keepWaiting)
                        {
                            yield return null;
                        }

                        break;
                    }
                    case YieldInstruction yieldInstruction:
                    {
                        switch (yieldInstruction)
                        {
                            case AsyncOperation ao:
                                yield return WaitAsyncOperation(ao);
                                break;
                            case WaitForSeconds wfs:
                                yield return WaitWaitForSeconds(wfs);
                                break;
                            default:
                                SetError(new NotSupportedException(
                                    $"{yieldInstruction.GetType().Name} is not supported."
                                ));
                                yield break;
                        }

                        break;
                    }
                    case IEffect effect:
                        yield return _effectRunner.RunEffect(effect, this);
                        break;
                    case IEnumerator e:
                        var ne = InnerEnumerator(e);
                        while (ne.MoveNext())
                        {
                            yield return null;
                        }

                        break;
                }
            }

            var childCoroutines = _childCoroutines.ToArray();
            while (childCoroutines.Any(c => !c.IsCompleted && c.IsCanceled && !c.IsError))
            {
                yield return null;
            }

            _rootCoroutine._childCoroutines.Remove(this);
        }

        private static IEnumerator WaitAsyncOperation(AsyncOperation asyncOperation)
        {
            while (!asyncOperation.isDone)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitWaitForSeconds(WaitForSeconds waitForSeconds)
        {
            var second = waitForSeconds.GetSeconds();
            var elapsed = 0.0f;
            while (true)
            {
                yield return null;

                elapsed += Time.deltaTime;
                if (elapsed >= second)
                {
                    break;
                }
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
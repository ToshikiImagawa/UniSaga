// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;

namespace UniSaga.Core
{
    internal class EffectRunner<TState> : IDisposable
    {
        [NotNull] private readonly Func<TState> _getState;
        [NotNull] private readonly Func<object, object> _dispatch;
        [NotNull] private readonly IObservable<object> _subject;

        private bool _isDisposed;
        private readonly object _disposablesLock = new object();
        [NotNull] private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public EffectRunner(
            [NotNull] Func<TState> getState,
            [NotNull] Func<object, object> dispatch,
            [NotNull] IObservable<object> subject
        )
        {
            _getState = getState;
            _dispatch = dispatch;
            _subject = subject;
        }

        public SagaTask Run([NotNull] IEnumerator effectsOrNull)
        {
            return Run(effectsOrNull, null);
        }

        private SagaTask Run([NotNull] IEnumerator effectsOrNull, [CanBeNull] SagaTask parentSagaTask)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var sagaTask = new SagaTask(cancellationTokenSource, parentSagaTask);
            AddDisposable(cancellationTokenSource);
            sagaTask.Coroutine = UniSagaRunner.Instance.StartCoroutine(Inner());
            return sagaTask;

            IEnumerator Inner()
            {
                yield return ConsumeEnumerator(effectsOrNull, sagaTask);
                while (!sagaTask.TryComplete()) yield return null;

                RemoveDisposable(cancellationTokenSource);
            }
        }

        private IEnumerator ConsumeEnumerator(
            [NotNull] IEnumerator effectsOrNull,
            SagaTask sagaTask
        )
        {
            if (_isDisposed) yield break;

            while (effectsOrNull.MoveNext())
            {
                if (sagaTask.IsCanceled) yield break;
                var current = effectsOrNull.Current;
                switch (current)
                {
                    case null:
                    {
                        yield return null;
                        break;
                    }
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
                                sagaTask.SetError(new NotSupportedException(
                                    $"{yieldInstruction.GetType().Name} is not supported."
                                ));
                                yield break;
                        }

                        break;
                    }
                    case IEffect effect:
                    {
                        switch (effect)
                        {
                            case AllEffect allEffect:
                            {
                                yield return WaitAllEffect(allEffect, sagaTask);
                                break;
                            }
                            case RaceEffect raceEffect:
                            {
                                yield return WaitRaceEffect(raceEffect, sagaTask);
                                break;
                            }
                            case CallEffect callEffect:
                            {
                                yield return WaitCallEffect(callEffect, sagaTask);
                                break;
                            }
                            case CancelEffect cancelEffect:
                            {
                                RunCancelEffect(cancelEffect, sagaTask);
                                break;
                            }
                            case SelectEffect selectEffect:
                            {
                                RunSelectEffect(selectEffect, sagaTask);
                                break;
                            }
                            case PutEffect putEffect:
                            {
                                RunPutEffect(putEffect, sagaTask);
                                break;
                            }
                            case TakeEffect takeEffect:
                            {
                                yield return WaitTakeEffect(takeEffect, sagaTask);
                                break;
                            }
                            case ForkEffect forkEffect:
                            {
                                RunForkEffect(forkEffect, sagaTask);
                                break;
                            }
                            case JoinEffect joinEffect:
                            {
                                yield return WaitJoinEffect(joinEffect, sagaTask);
                                break;
                            }
                        }

                        break;
                    }
                    case IEnumerator enumerator:
                    {
                        var e = ConsumeEnumerator(enumerator, sagaTask);
                        while (e.MoveNext())
                        {
                            yield return null;
                        }

                        break;
                    }
                    default:
                    {
                        sagaTask.SetError(new NotSupportedException(
                            $"{current.GetType().Name} is not supported."
                        ));
                        yield break;
                    }
                }
            }
        }

        private IEnumerator WaitAllEffect(IEffect effect, SagaTask parentSagaTask)
        {
            SagaTask[] tasks;
            try
            {
                tasks = ConvertEffectsTasks(effect, parentSagaTask);
            }
            catch (Exception error)
            {
                parentSagaTask.SetError(error);
                yield break;
            }

            while (!tasks.All(task => task.IsCompleted || task.IsCanceled))
            {
                yield return null;
            }

            foreach (var task in tasks)
            {
                task.Cancel();
            }
        }

        private SagaTask[] ConvertEffectsTasks(
            IEffect effect,
            SagaTask parentSagaTask
        )
        {
            if (!effect.Combinator) return Array.Empty<SagaTask>();
            if (!(effect.Payload is ICombinatorEffectDescriptor descriptor)) throw new InvalidOperationException();
            var tasks = descriptor.Effects.Select(payloadEffect =>
            {
                return Run(InnerTask(), parentSagaTask);

                IEnumerator InnerTask()
                {
                    yield return payloadEffect;
                }
            }).ToArray();
            return tasks;
        }

        private IEnumerator WaitRaceEffect(IEffect effect, SagaTask parentSagaTask)
        {
            SagaTask[] tasks;
            try
            {
                tasks = ConvertEffectsTasks(effect, parentSagaTask);
            }
            catch (Exception error)
            {
                parentSagaTask.SetError(error);
                yield break;
            }

            while (!tasks.Any(task => task.IsCompleted || task.IsCanceled))
            {
                yield return null;
            }

            foreach (var task in tasks)
            {
                task.Cancel();
            }
        }

        private IEnumerator WaitCallEffect(IEffect effect, SagaTask task)
        {
            if (!(effect.Payload is CallEffect.Descriptor descriptor))
            {
                task.SetError(new InvalidOperationException());
                yield break;
            }

            var args = new[] { (object)task }.Concat(descriptor.Args).ToArray();
            var effectsOrNull = descriptor.Fn(args);
            yield return ConsumeEnumerator(effectsOrNull, task);
        }

        private static void RunCancelEffect(IEffect effect, SagaTask sagaTask)
        {
            if (!(effect.Payload is CancelEffect.Descriptor descriptor))
            {
                sagaTask.SetError(new InvalidOperationException());
                return;
            }

            var task = descriptor.Task ?? sagaTask;
            task.Cancel();
        }

        private void RunSelectEffect(IEffect effect, SagaTask task)
        {
            if (!(effect.Payload is SelectEffect.Descriptor descriptor))
            {
                task.SetError(new InvalidOperationException());
                return;
            }

            var value = descriptor.Selector(_getState(), descriptor.Args);
            descriptor.SetResultValue(value);
        }

        private void RunPutEffect(IEffect effect, SagaTask task)
        {
            if (!(effect.Payload is PutEffect.Descriptor descriptor))
            {
                task.SetError(new InvalidOperationException());
                return;
            }

            _dispatch(descriptor.Action);
        }

        private IEnumerator WaitTakeEffect(IEffect effect, SagaTask task)
        {
            if (!(effect.Payload is TakeEffect.Descriptor descriptor))
            {
                task.SetError(new InvalidOperationException());
                yield break;
            }

            var isTaken = false;
            using (_subject.Where(descriptor.Pattern).Subscribe(new SimpleObserver<object>(_ => { isTaken = true; })))
            {
                while (!isTaken) yield return null;
            }
        }

        private void RunForkEffect(IEffect effect, SagaTask parentSagaTask)
        {
            if (!(effect.Payload is ForkEffect.Descriptor descriptor))
            {
                parentSagaTask.SetError(new InvalidOperationException());
                return;
            }

            if (!(descriptor.Context is InternalSaga saga))
            {
                parentSagaTask.SetError(new InvalidOperationException());
                return;
            }

            var sagaTask = Run(saga(descriptor.Args), parentSagaTask);
            descriptor.SetResultValue?.Invoke(sagaTask);
        }

        private static IEnumerator WaitJoinEffect(IEffect effect, SagaTask task)
        {
            if (!(effect.Payload is JoinEffect.Descriptor descriptor))
            {
                task.SetError(new InvalidOperationException());
                yield break;
            }

            if (!(descriptor.Context is SagaTask sagaTask))
            {
                task.SetError(new InvalidOperationException());
                yield break;
            }

            while (!sagaTask.IsCompleted && !sagaTask.IsCanceled)
            {
                yield return null;
            }
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

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            foreach (var disposable in GetDisposables())
            {
                disposable?.Dispose();
            }
        }

        private void AddDisposable(IDisposable disposable)
        {
            if (_isDisposed)
            {
                disposable?.Dispose();
                return;
            }

            lock (_disposablesLock)
            {
                _disposables.Add(disposable);
            }
        }

        private void RemoveDisposable(IDisposable disposable)
        {
            lock (_disposablesLock)
            {
                _disposables.Remove(disposable);
            }
        }

        private IEnumerable<IDisposable> GetDisposables()
        {
            IDisposable[] disposables;
            lock (_disposablesLock)
            {
                disposables = _disposables.ToArray();
                _disposables.Clear();
            }

            return disposables;
        }

        private class SimpleObserver<T> : IObserver<T>
        {
            private readonly Action _onCompleted;
            private readonly Action<Exception> _onError;
            private readonly Action<T> _onNext;

            public SimpleObserver(
                Action<T> onNext = null,
                Action onCompleted = null,
                Action<Exception> onError = null)
            {
                _onCompleted = onCompleted;
                _onError = onError;
                _onNext = onNext;
            }

            public void OnCompleted()
            {
                _onCompleted?.Invoke();
            }

            public void OnError(Exception error)
            {
                _onError?.Invoke(error);
            }

            public void OnNext(T value)
            {
                _onNext?.Invoke(value);
            }
        }
    }
}
// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections;
using System.Linq;
using JetBrains.Annotations;

namespace UniSaga.Core
{
    internal class EffectRunner<TState> : IEffectRunner
    {
        [NotNull] private readonly Func<TState> _getState;
        [NotNull] private readonly Func<object, object> _dispatch;
        [NotNull] private readonly IObservable<object> _subject;

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

        public IEnumerator RunEffect(IEffect effect, SagaCoroutine coroutine)
        {
            switch (effect)
            {
                case AllEffect allEffect:
                {
                    yield return WaitAllEffect(allEffect, coroutine);
                    break;
                }
                case RaceEffect raceEffect:
                {
                    yield return WaitRaceEffect(raceEffect, coroutine);
                    break;
                }
                case CallEffect callEffect:
                {
                    yield return WaitCallEffect(callEffect, coroutine);
                    break;
                }
                case CancelEffect cancelEffect:
                {
                    RunCancelEffect(cancelEffect, coroutine);
                    break;
                }
                case SelectEffect selectEffect:
                {
                    RunSelectEffect(selectEffect);
                    break;
                }
                case PutEffect putEffect:
                {
                    RunPutEffect(putEffect);
                    break;
                }
                case TakeEffect takeEffect:
                {
                    yield return WaitTakeEffect(takeEffect);
                    break;
                }
                case ForkEffect forkEffect:
                {
                    RunForkEffect(forkEffect, coroutine);
                    break;
                }
                case JoinEffect joinEffect:
                {
                    yield return WaitJoinEffect(joinEffect, coroutine);
                    break;
                }
            }
        }

        private IEnumerator WaitAllEffect(AllEffect effect, SagaCoroutine coroutine)
        {
            SagaCoroutine[] coroutines;
            try
            {
                coroutines = ConvertEffectsTasks(effect.EffectDescriptor, coroutine);
            }
            catch (Exception error)
            {
                coroutine.SetError(error);
                yield break;
            }

            while (!coroutines.All(task => task.IsCompleted || task.IsCanceled))
            {
                yield return null;
            }

            foreach (var task in coroutines)
            {
                task.RequestCancel();
            }
        }

        private static SagaCoroutine[] ConvertEffectsTasks(
            CombinatorEffectDescriptor descriptor,
            SagaCoroutine coroutine
        )
        {
            var coroutines = descriptor.Effects.Select(payloadEffect =>
            {
                return coroutine.StartCoroutine(InnerTask());

                IEnumerator InnerTask()
                {
                    yield return payloadEffect;
                }
            }).ToArray();
            return coroutines;
        }

        private static IEnumerator WaitRaceEffect(RaceEffect effect, SagaCoroutine coroutine)
        {
            SagaCoroutine[] coroutines;
            try
            {
                coroutines = ConvertEffectsTasks(effect.EffectDescriptor, coroutine);
            }
            catch (Exception error)
            {
                coroutine.SetError(error);
                yield break;
            }

            while (!coroutines.Any(task => task.IsCompleted || task.IsCanceled))
            {
                yield return null;
            }

            foreach (var c in coroutines)
            {
                c.RequestCancel();
            }
        }

        private static IEnumerator WaitCallEffect(CallEffect effect, SagaCoroutine coroutine)
        {
            var descriptor = effect.EffectDescriptor;
            var args = new[] { (object)coroutine }.Concat(descriptor.Args).ToArray();
            yield return descriptor.Fn(args);
        }

        private static void RunCancelEffect(CancelEffect effect, SagaCoroutine sagaCoroutine)
        {
            var descriptor = effect.EffectDescriptor;
            var coroutine = descriptor.Coroutine ?? sagaCoroutine;
            coroutine.RequestCancel();
        }

        private void RunSelectEffect(SelectEffect effect)
        {
            var descriptor = effect.EffectDescriptor;
            var value = descriptor.Selector(_getState(), descriptor.Args);
            descriptor.SetResultValue(value);
        }

        private void RunPutEffect(PutEffect effect)
        {
            var descriptor = effect.EffectDescriptor;
            _dispatch(descriptor.Action);
        }

        private IEnumerator WaitTakeEffect(TakeEffect effect)
        {
            var descriptor = effect.EffectDescriptor;
            var isTaken = false;
            using (_subject.Where(descriptor.Pattern).Subscribe(new SimpleObserver<object>(_ => { isTaken = true; })))
            {
                while (!isTaken) yield return null;
            }
        }

        private static void RunForkEffect(ForkEffect effect, SagaCoroutine sagaCoroutine)
        {
            var descriptor = effect.EffectDescriptor;
            if (!(descriptor.Context is InternalSaga saga))
            {
                sagaCoroutine.SetError(new InvalidOperationException());
                return;
            }

            var coroutine = sagaCoroutine.StartCoroutine(saga(descriptor.Args));
            descriptor.SetResultValue?.Invoke(coroutine);
        }

        private static IEnumerator WaitJoinEffect(JoinEffect effect, SagaCoroutine sagaCoroutine)
        {
            var descriptor = effect.EffectDescriptor;
            if (!(descriptor.Context is SagaCoroutine coroutine))
            {
                sagaCoroutine.SetError(new InvalidOperationException());
                yield break;
            }

            while (!coroutine.IsCompleted && !coroutine.IsCanceled)
            {
                yield return null;
            }
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

    internal interface IEffectRunner
    {
        IEnumerator RunEffect(IEffect effect, SagaCoroutine coroutine);
    }
}
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
            return effect switch
            {
                AllEffect allEffect => WaitAllEffect(allEffect, coroutine),
                RaceEffect raceEffect => WaitRaceEffect(raceEffect, coroutine),
                CallEffect callEffect => WaitCallEffect(callEffect, coroutine),
                CancelEffect cancelEffect => RunCancelEffect(cancelEffect, coroutine),
                SelectEffect selectEffect => RunSelectEffect(selectEffect),
                PutEffect putEffect => RunPutEffect(putEffect),
                TakeEffect takeEffect => WaitTakeEffect(takeEffect),
                ForkEffect forkEffect => RunForkEffect(forkEffect, coroutine),
                JoinEffect joinEffect => WaitJoinEffect(joinEffect, coroutine),
                _ => Enumerator.Empty
            };
        }

        private static IEnumerator WaitAllEffect(AllEffect effect, SagaCoroutine coroutine)
        {
            SagaCoroutine[] coroutines;
            try
            {
                coroutines = ConvertEffectCoroutines(effect.EffectDescriptor, coroutine);
            }
            catch (Exception error)
            {
                coroutine.SetError(error);
                return Enumerator.Empty;
            }

            return Inner();

            IEnumerator Inner()
            {
                while (!coroutines.All(sagaCoroutine => sagaCoroutine.IsCompleted || sagaCoroutine.IsCanceled))
                {
                    yield return null;
                }

                foreach (var sagaCoroutine in coroutines)
                {
                    sagaCoroutine.RequestCancel();
                }
            }
        }

        private static IEnumerator WaitRaceEffect(RaceEffect effect, SagaCoroutine coroutine)
        {
            SagaCoroutine[] coroutines;
            try
            {
                coroutines = ConvertEffectCoroutines(effect.EffectDescriptor, coroutine);
            }
            catch (Exception error)
            {
                coroutine.SetError(error);
                return Enumerator.Empty;
            }

            return Inner();

            IEnumerator Inner()
            {
                while (!coroutines.Any(sagaCoroutine => sagaCoroutine.IsCompleted || sagaCoroutine.IsCanceled))
                {
                    yield return null;
                }

                foreach (var c in coroutines)
                {
                    c.RequestCancel();
                }
            }
        }

        private static SagaCoroutine[] ConvertEffectCoroutines(
            CombinatorEffectDescriptor descriptor,
            SagaCoroutine coroutine
        )
        {
            var coroutines = descriptor.Effects.Select(payloadEffect =>
            {
                return coroutine.StartCoroutine(InnerCoroutine());

                IEnumerator InnerCoroutine()
                {
                    yield return payloadEffect;
                }
            }).ToArray();
            return coroutines;
        }

        private static IEnumerator WaitCallEffect(CallEffect effect, SagaCoroutine coroutine)
        {
            var descriptor = effect.EffectDescriptor;
            var args = new[] { (object)coroutine }.Concat(descriptor.Args).ToArray();

            return Inner();

            IEnumerator Inner()
            {
                yield return descriptor.Fn(args);
            }
        }

        private static IEnumerator RunCancelEffect(CancelEffect effect, SagaCoroutine sagaCoroutine)
        {
            var descriptor = effect.EffectDescriptor;
            var coroutine = descriptor.Coroutine ?? sagaCoroutine;
            coroutine.RequestCancel();
            return Enumerator.Empty;
        }

        private IEnumerator RunSelectEffect(SelectEffect effect)
        {
            var descriptor = effect.EffectDescriptor;
            var value = descriptor.Selector(_getState(), descriptor.Args);
            descriptor.SetResultValue(value);
            return Enumerator.Empty;
        }

        private IEnumerator RunPutEffect(PutEffect effect)
        {
            var descriptor = effect.EffectDescriptor;
            _dispatch(descriptor.Action);
            return Enumerator.Empty;
        }

        private IEnumerator WaitTakeEffect(TakeEffect effect)
        {
            var descriptor = effect.EffectDescriptor;
            var isTaken = false;
            var disposable = _subject.Where(descriptor.Pattern)
                .Subscribe(new SimpleObserver<object>(_ => { isTaken = true; }));
            return Inner();

            IEnumerator Inner()
            {
                using (disposable)
                {
                    while (!isTaken) yield return null;
                }
            }
        }

        private static IEnumerator RunForkEffect(ForkEffect effect, SagaCoroutine sagaCoroutine)
        {
            var descriptor = effect.EffectDescriptor;
            if (!(descriptor.Context is InternalSaga saga))
            {
                sagaCoroutine.SetError(new InvalidOperationException());
                return Enumerator.Empty;
            }

            var coroutine = sagaCoroutine.StartCoroutine(saga(descriptor.Args));
            descriptor.SetResultValue?.Invoke(coroutine);
            return Enumerator.Empty;
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
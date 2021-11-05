// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UniRedux;
using UniSaga.Core;

namespace UniSaga
{
    public delegate IEnumerator<IEffect> Saga();

    public class UniSagaMiddleware<TState> : IDisposable
    {
        private Func<TState> _getState;
        private Func<object, object> _dispatch;
        private readonly Subject<object> _subject = new Subject<object>();

        public Func<Dispatcher, Dispatcher> Middleware(IStore<TState> store)
        {
            _getState = store.GetState;
            _dispatch = store.Dispatch;
            return next => action =>
            {
                _subject.OnNext(action);
                return next(action);
            };
        }

        public async void Run(Saga rootSaga)
        {
            if (rootSaga == null) throw new InvalidOperationException();
            if (_getState == null) throw new InvalidOperationException();
            if (_dispatch == null) throw new InvalidOperationException();

            await RunSaga(rootSaga(), _getState, _dispatch, _subject);
        }

        private static async UniTask RunSaga(
            [NotNull] IEnumerator<IEffect> enumerator,
            [NotNull] Func<TState> getState,
            [NotNull] Func<object, object> dispatch,
            [NotNull] IObservable<object> subject)
        {
            while (enumerator.MoveNext())
            {
                var effect = enumerator.Current;
                if (effect == null)
                {
                    await UniTask.DelayFrame(1);
                    continue;
                }

                switch (effect)
                {
                    case CallEffect callEffect:
                    {
                        var value = await callEffect.Payload.Fn(callEffect.Payload.Args);
                        callEffect.Payload.SetResultValue?.Invoke(value);
                        continue;
                    }
                    case SelectEffect selectEffect:
                    {
                        var value = selectEffect.Payload.Selector(getState(), selectEffect.Payload.Args);
                        selectEffect.Payload.SetResultValue(value);
                        continue;
                    }
                    case PutEffect putEffect:
                    {
                        dispatch(putEffect.Payload.Action);
                        continue;
                    }
                    case TakeEffect takeEffect:
                    {
                        await subject.Where(takeEffect.Payload.Pattern).ToUniTask(true);
                        continue;
                    }
                    case ForkEffect forkEffect:
                    {
                        UniTask.Run(async () =>
                        {
                            var newEnumerator = (IEnumerator<IEffect>)await forkEffect.Payload.Fn(forkEffect.Payload.Args);
                            await RunSaga(newEnumerator, getState, dispatch, subject);
                        });
                        continue;
                    }
                }
            }
        }

        public void Dispose()
        {
            _subject?.Dispose();
        }
    }

    internal static class RxExtensions
    {
        public static IObservable<TResult> Where<TResult>(this IObservable<TResult> self, Func<TResult, bool> filter)
        {
            return new Filter<TResult>(filter, self);
        }

        private class Filter<TResult> : IObservable<TResult>
        {
            private readonly Func<TResult, bool> _filter;
            private readonly Subject<TResult> _subject = new Subject<TResult>();
            private readonly IDisposable _disposable;

            public Filter(Func<TResult, bool> filter, IObservable<TResult> original)
            {
                _filter = filter;
                _disposable = original.Subscribe(OnNext, OnError, OnCompleted);
            }

            public IDisposable Subscribe(IObserver<TResult> observer)
            {
                return _subject.Subscribe(observer);
            }

            private void OnCompleted()
            {
                _subject.OnCompleted();
                _disposable.Dispose();
            }

            private void OnError(Exception error)
            {
                _subject.OnError(error);
                _disposable.Dispose();
            }

            private void OnNext(TResult value)
            {
                if (!_filter(value)) return;
                _subject.OnNext(value);
            }
        }
    }
}
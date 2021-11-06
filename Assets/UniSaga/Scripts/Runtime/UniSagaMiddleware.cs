// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using UniRedux;
using UniSaga.Core;

namespace UniSaga
{
    public class UniSagaMiddleware<TState> : IDisposable
    {
        private Func<TState> _getState;
        private Func<object, object> _dispatch;
        private readonly Subject<object> _subject = new Subject<object>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

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

        public SagaTask Run(Saga rootSaga)
        {
            if (rootSaga == null) throw new InvalidOperationException();
            if (_getState == null) throw new InvalidOperationException();
            if (_dispatch == null) throw new InvalidOperationException();
            if (_cancellationTokenSource.Token.IsCancellationRequested) return null;
            var sagaTask = new SagaTask(_cancellationTokenSource);
            Run(
                _getState,
                _dispatch,
                _subject,
                sagaTask,
                rootSaga()
            );
            return sagaTask;
        }

        public void Dispose()
        {
            _subject?.Dispose();
        }

        private static async void Run(
            [NotNull] Func<TState> getState,
            [NotNull] Func<object, object> dispatch,
            [NotNull] IObservable<object> subject,
            [NotNull] SagaTask sagaTask,
            IEnumerator<IEffect> effects
        )
        {
            using var runner = new EffectRunner<TState>(getState, dispatch, subject);
            await runner.Run(
                effects,
                sagaTask
            );
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
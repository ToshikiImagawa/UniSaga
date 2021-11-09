// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections;
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
            return Run(_ => rootSaga());
        }

        public SagaTask Run<TArgument>(
            Saga<TArgument> rootSaga,
            TArgument argument
        )
        {
            return Run(args => rootSaga((TArgument)args[0]), argument);
        }

        public SagaTask Run<TArgument1, TArgument2>(
            Saga<TArgument1, TArgument2> rootSaga,
            TArgument1 argument1,
            TArgument2 argument2
        )
        {
            return Run(
                args => rootSaga(
                    (TArgument1)args[0],
                    (TArgument2)args[1]
                ),
                argument1,
                argument2
            );
        }

        public SagaTask Run<TArgument1, TArgument2, TArgument3>(
            Saga<TArgument1, TArgument2, TArgument3> rootSaga,
            TArgument1 argument1,
            TArgument2 argument2,
            TArgument3 argument3
        )
        {
            return Run(
                args => rootSaga(
                    (TArgument1)args[0],
                    (TArgument2)args[1],
                    (TArgument3)args[2]
                ),
                argument1,
                argument2,
                argument3
            );
        }

        private SagaTask Run(InternalSaga rootSaga, params object[] arguments)
        {
            if (rootSaga == null) throw new InvalidOperationException();
            if (_getState == null) throw new InvalidOperationException();
            if (_dispatch == null) throw new InvalidOperationException();
            return Run(
                _getState,
                _dispatch,
                _subject,
                rootSaga(arguments ?? Array.Empty<object>())
            );
        }

        public void Dispose()
        {
            _subject?.Dispose();
        }

        private static SagaTask Run(
            [NotNull] Func<TState> getState,
            [NotNull] Func<object, object> dispatch,
            [NotNull] IObservable<object> subject,
            IEnumerator effectsOrNull
        )
        {
            var runner = new EffectRunner<TState>(getState, dispatch, subject);
            return runner.Run(effectsOrNull);
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
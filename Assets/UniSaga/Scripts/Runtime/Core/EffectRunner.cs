// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

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

        public async UniTask Run(
            [NotNull] IEnumerator<IEffect> enumerator,
            SagaTask sagaTask
        )
        {
            var token = sagaTask.Token;
            if (_isDisposed) return;
            while (enumerator.MoveNext())
            {
                token.ThrowIfCancellationRequested();

                var effect = enumerator.Current;
                if (effect == null)
                {
                    await UniTask.DelayFrame(1, cancellationToken: token);
                    continue;
                }

                switch (effect)
                {
                    case CallEffect callEffect:
                    {
                        await RunCallEffect(callEffect, token);
                        continue;
                    }
                    case SelectEffect selectEffect:
                    {
                        RunSelectEffect(selectEffect);
                        continue;
                    }
                    case PutEffect putEffect:
                    {
                        RunPutEffect(putEffect);
                        continue;
                    }
                    case TakeEffect takeEffect:
                    {
                        await RunTakeEffect(takeEffect, token);
                        continue;
                    }
                    case ForkEffect forkEffect:
                    {
                        RunForkEffect(forkEffect, sagaTask);
                        continue;
                    }
                    case JoinEffect joinEffect:
                    {
                        await RunJoinEffect(joinEffect, token);
                        continue;
                    }
                }
            }

            await UniTask.WaitUntil(sagaTask.TryComplete, cancellationToken: token);
        }

        private static async UniTask RunCallEffect(CallEffect effect, CancellationToken token)
        {
            var value = await effect.Payload.Fn(effect.Payload.Args, token);
            effect.Payload.SetResultValue?.Invoke(value);
        }

        private void RunSelectEffect(SelectEffect effect)
        {
            var value = effect.Payload.Selector(_getState(), effect.Payload.Args);
            effect.Payload.SetResultValue(value);
        }

        private void RunPutEffect(PutEffect effect)
        {
            _dispatch(effect.Payload.Action);
        }

        private async UniTask RunTakeEffect(TakeEffect effect, CancellationToken token)
        {
            await _subject.Where(effect.Payload.Pattern).ToUniTask(true, token);
        }

        private async void RunForkEffect(ForkEffect effect, SagaTask parentSagaTask)
        {
            using var forkCts = new CancellationTokenSource();
            var sagaTask = new SagaTask(forkCts, parentSagaTask);
            AddDisposable(forkCts);
            effect.Payload.SetResultValue?.Invoke(sagaTask);
            if (!(effect.Payload.Context is Saga saga)) throw new InvalidOperationException();
            await Run(saga(), sagaTask);
            RemoveDisposable(forkCts);
        }

        private static async UniTask RunJoinEffect(JoinEffect effect, CancellationToken token)
        {
            if (!(effect.Payload.Context is SagaTask sagaTask)) throw new InvalidOperationException();
            await UniTask.WaitUntil(() => sagaTask.IsCompleted, cancellationToken: token);
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
    }
}
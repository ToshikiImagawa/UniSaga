// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
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
            if (_isDisposed) return;
            while (enumerator.MoveNext())
            {
                await Run(enumerator.Current, sagaTask);
            }

            await UniTask.WaitUntil(sagaTask.TryComplete, cancellationToken: sagaTask.Token);
        }

        private async UniTask Run(
            [CanBeNull] IEffect effect,
            SagaTask sagaTask
        )
        {
            sagaTask.Token.ThrowIfCancellationRequested();
            if (effect == null)
            {
                await UniTask.DelayFrame(1, cancellationToken: sagaTask.Token);
                return;
            }

            switch (effect)
            {
                case AllEffect allEffect:
                {
                    await RunAllEffect(allEffect, sagaTask);
                    return;
                }
                case RaceEffect raceEffect:
                {
                    await RunRaceEffect(raceEffect, sagaTask);
                    return;
                }
                case CallEffect callEffect:
                {
                    await RunCallEffect(callEffect, sagaTask.Token);
                    return;
                }
                case CancelEffect cancelEffect:
                {
                    RunCancelEffect(cancelEffect, sagaTask);
                    return;
                }
                case SelectEffect selectEffect:
                {
                    RunSelectEffect(selectEffect);
                    return;
                }
                case PutEffect putEffect:
                {
                    RunPutEffect(putEffect);
                    return;
                }
                case TakeEffect takeEffect:
                {
                    await RunTakeEffect(takeEffect, sagaTask.Token);
                    return;
                }
                case ForkEffect forkEffect:
                {
                    RunForkEffect(forkEffect, sagaTask);
                    return;
                }
                case JoinEffect joinEffect:
                {
                    await RunJoinEffect(joinEffect, sagaTask.Token);
                    return;
                }
            }
        }

        private async UniTask RunAllEffect(IEffect effect, SagaTask parentSagaTask)
        {
            var tasks = ConvertEffectsTasks(effect, parentSagaTask);
            await UniTask
                .WhenAll(tasks)
                .WithCancellation(parentSagaTask.Token);
        }

        private UniTask[] ConvertEffectsTasks(
            IEffect effect,
            SagaTask parentSagaTask,
            ICollection<CancellationTokenSource> sources = null
        )
        {
            if (!effect.Combinator) return Array.Empty<UniTask>();
            if (!(effect.Payload is ICombinatorEffectDescriptor descriptor)) throw new InvalidOperationException();

            var tasks = descriptor.Effects.Select(async payloadEffect =>
            {
                using var forkCts = new CancellationTokenSource();
                var sagaTask = new SagaTask(forkCts, parentSagaTask);
                AddDisposable(forkCts);
                sources?.Add(forkCts);

                try
                {
                    await Run(payloadEffect, sagaTask);
                }
                catch (Exception e)
                {
                    sagaTask.SetError(e);
                }
                finally
                {
                    RemoveDisposable(forkCts);
                    sources?.Remove(forkCts);
                }
            }).ToArray();
            return tasks;
        }

        private async UniTask RunRaceEffect(IEffect effect, SagaTask parentSagaTask)
        {
            var sources = new List<CancellationTokenSource>();
            var tasks = ConvertEffectsTasks(effect, parentSagaTask, sources);
            await UniTask
                .WhenAny(tasks)
                .WithCancellation(parentSagaTask.Token);
            foreach (var source in sources.ToArray())
            {
                source.Cancel();
            }
        }

        private static async UniTask RunCallEffect(CallEffect effect, CancellationToken token)
        {
            var value = await effect.Payload.Fn(effect.Payload.Args, token);
            effect.Payload.SetResultValue?.Invoke(value);
        }

        private static void RunCancelEffect(CancelEffect effect, SagaTask sagaTask)
        {
            var task = effect.Payload.Task ?? sagaTask;
            task.Cancel();
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
            if (!(effect.Payload.Context is InternalSaga saga)) throw new InvalidOperationException();
            using var forkCts = new CancellationTokenSource();
            var sagaTask = new SagaTask(forkCts, parentSagaTask);
            AddDisposable(forkCts);
            effect.Payload.SetResultValue?.Invoke(sagaTask);
            try
            {
                await Run(saga(effect.Payload.Args), sagaTask);
            }
            catch (Exception e)
            {
                sagaTask.SetError(e);
            }
            finally
            {
                RemoveDisposable(forkCts);
            }
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
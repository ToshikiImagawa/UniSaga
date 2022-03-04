using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UniRedux;
using UniSaga.Core;
using UniSaga.Plugin;

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
    public delegate IEnumerator Saga<in TArgument1, in TArgument2, in TArgument3, in TArgument4>(
        TArgument1 argument1,
        TArgument2 argument2,
        TArgument3 argument3,
        TArgument4 argument4
    );

    public class SagaCoroutine : IPlayerLoopItem
    {
        [NotNull] private readonly List<SagaCoroutine> _childCoroutines = new List<SagaCoroutine>();
        [NotNull] private readonly SagaCoroutine _rootCoroutine;
        [NotNull] private readonly IEffectRunner _effectRunner;
        [NotNull] private readonly IEnumerator _enumerator;
        [NotNull] private readonly SingleReactiveProperty<Exception> _onError = new SingleReactiveProperty<Exception>();

        [NotNull] private readonly SingleReactiveProperty<VoidMessage> _onCanceled =
            new SingleReactiveProperty<VoidMessage>();

        [NotNull] private readonly SingleReactiveProperty<VoidMessage> _onCompleted =
            new SingleReactiveProperty<VoidMessage>();

        private bool _requestCancel;
        private readonly object _lockObj = new object();

        // VisibleForTesting
        internal SagaCoroutine(
            [NotNull] IEffectRunner effectRunner,
            IEnumerator enumerator
        )
        {
            _rootCoroutine = this;
            _effectRunner = effectRunner;
            _enumerator = InnerEnumerator(enumerator);
        }

        // VisibleForTesting
        internal SagaCoroutine(
            [NotNull] SagaCoroutine sagaCoroutine,
            IEnumerator enumerator
        )
        {
            _rootCoroutine = sagaCoroutine._rootCoroutine;
            _effectRunner = sagaCoroutine._effectRunner;
            _enumerator = InnerEnumerator(enumerator);
            _rootCoroutine._childCoroutines.Add(this);
        }

        public virtual bool IsError { get; private set; }
        public virtual bool IsCanceled { get; private set; }
        public virtual bool IsCompleted { get; private set; }

        public IObservable<Exception> OnError => _onError;
        public IObservable<VoidMessage> OnCanceled => _onCanceled;
        public IObservable<VoidMessage> OnCompleted => _onCompleted;

        internal void RequestCancel()
        {
            _requestCancel = true;
        }

        internal virtual void SetError(Exception error)
        {
            lock (_lockObj)
            {
                if (IsCompleted) return;
                IsError = true;
                IsCompleted = true;
                _onError.Value = error;
            }
        }

        bool IPlayerLoopItem.MoveNext()
        {
            lock (_lockObj)
            {
                if (IsCompleted) return false;
                if (_requestCancel)
                {
                    IsCanceled = true;
                    IsCompleted = true;
                    _onCanceled.Value = VoidMessage.Default;
                    return false;
                }

                try
                {
                    if (_enumerator.MoveNext()) return true;
                }
                catch (Exception error)
                {
                    IsError = true;
                    IsCompleted = true;
                    _onError.Value = error;
                    return false;
                }

                IsCompleted = true;
                _onCompleted.Value = VoidMessage.Default;
                return false;
            }
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

        internal virtual SagaCoroutine StartCoroutine([NotNull] IEnumerator enumerator)
        {
            var coroutine = new SagaCoroutine(this, enumerator);
            PlayerLoopHelper.AddAction(coroutine);
            return coroutine;
        }

        private IEnumerator RunEffect(IEffect effect) => _effectRunner.RunEffect(effect, this);

        private IEnumerator InnerEnumerator(IEnumerator enumerator)
        {
            var flatEnumerator = enumerator.Flat();
            while (flatEnumerator.MoveNext())
            {
                var current = flatEnumerator.Current;
                switch (current)
                {
                    case null:
                    {
                        yield return null;
                        break;
                    }
                    case IEffect effect:
                    {
                        var ne = InnerEnumerator(RunEffect(effect));
                        while (ne.MoveNext()) yield return null;
                        break;
                    }
                    default:
                    {
                        throw new NotSupportedException($"{current.GetType().Name} is not supported.");
                    }
                }
            }

            var childCoroutines = _childCoroutines.ToArray();
            while (childCoroutines.Any(c => !c.IsCompleted && c.IsCanceled && !c.IsError))
            {
                yield return null;
            }

            _rootCoroutine._childCoroutines.Remove(this);
        }
    }
}
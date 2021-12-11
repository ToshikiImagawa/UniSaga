// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections;
using System.Linq;
using JetBrains.Annotations;

namespace UniSaga.Core
{
    internal delegate IEnumerator InternalSaga(params object[] arguments);

    internal abstract class CombinatorEffect : IEffect
    {
    }

    internal abstract class SimpleEffect : IEffect
    {
    }

    internal sealed class CombinatorEffectDescriptor
    {
        public CombinatorEffectDescriptor(IEffect[] effects)
        {
            Effects = effects;
        }

        public IEffect[] Effects { get; }
    }

    internal sealed class RaceEffect : CombinatorEffect
    {
        public RaceEffect(CombinatorEffectDescriptor payload)
        {
            EffectDescriptor = payload;
        }

        public CombinatorEffectDescriptor EffectDescriptor { get; }
    }

    internal sealed class AllEffect : CombinatorEffect
    {
        public AllEffect(CombinatorEffectDescriptor payload)
        {
            EffectDescriptor = payload;
        }

        public CombinatorEffectDescriptor EffectDescriptor { get; }
    }

    internal sealed class JoinEffect : SimpleEffect
    {
        public JoinEffect(Descriptor payload)
        {
            EffectDescriptor = payload;
        }

        public Descriptor EffectDescriptor { get; }

        public class Descriptor : CallEffect.Descriptor
        {
            public Descriptor(SagaCoroutine sagaCoroutine) : base(
                sagaCoroutine,
                a => Empty(),
                Array.Empty<object>(),
                null
            )
            {
            }

            private static IEnumerator Empty()
            {
                yield break;
            }
        }
    }

    internal sealed class ForkEffect : SimpleEffect
    {
        public ForkEffect([NotNull] Descriptor payload)
        {
            EffectDescriptor = payload;
        }

        public Descriptor EffectDescriptor { get; }

        public class Descriptor : CallEffect.Descriptor
        {
            public Descriptor(
                [NotNull] object context,
                [NotNull] object[] args,
                [CanBeNull] Action<object> setResultValue
            ) : base(
                context,
                a => Empty(),
                args,
                setResultValue
            )
            {
            }

            private static IEnumerator Empty()
            {
                yield break;
            }
        }
    }

    internal sealed class TakeEffect : SimpleEffect
    {
        public TakeEffect([NotNull] Descriptor payload)
        {
            EffectDescriptor = payload;
        }

        public Descriptor EffectDescriptor { get; }

        public class Descriptor
        {
            public Descriptor(Func<object, bool> pattern)
            {
                Pattern = pattern;
            }

            public Func<object, bool> Pattern { get; }
        }
    }

    internal sealed class PutEffect : SimpleEffect
    {
        public PutEffect([NotNull] Descriptor innerPayload)
        {
            EffectDescriptor = innerPayload;
        }

        public Descriptor EffectDescriptor { get; }

        public class Descriptor
        {
            public Descriptor(object action)
            {
                Action = action;
            }

            public object Action { get; }
        }
    }

    internal sealed class SelectEffect : SimpleEffect
    {
        public SelectEffect([NotNull] Descriptor descriptor)
        {
            EffectDescriptor = descriptor;
        }

        public Descriptor EffectDescriptor { get; }

        public class Descriptor
        {
            public Descriptor(
                [NotNull] Func<object, object[], object> selector,
                [NotNull] object[] args,
                [NotNull] Action<object> setResultValue)
            {
                Selector = selector;
                Args = args;
                SetResultValue = setResultValue;
            }

            [NotNull] public Func<object, object[], object> Selector { get; }
            [NotNull] public object[] Args { get; }
            [NotNull] public Action<object> SetResultValue { get; }
        }
    }

    internal sealed class CancelEffect : SimpleEffect
    {
        public CancelEffect(Descriptor payload)
        {
            EffectDescriptor = payload;
        }

        public Descriptor EffectDescriptor { get; }

        public class Descriptor

        {
            public Descriptor([CanBeNull] SagaCoroutine coroutine)
            {
                Coroutine = coroutine;
            }

            [CanBeNull] public SagaCoroutine Coroutine { get; }
        }
    }

    internal class CallEffect : SimpleEffect
    {
        public CallEffect([NotNull] Descriptor descriptor)
        {
            EffectDescriptor = descriptor;
        }

        public Descriptor EffectDescriptor { get; }

        public class Descriptor
        {
            public Descriptor(
                [NotNull] Func<object[], IEnumerator> fn,
                [NotNull] object[] args,
                [CanBeNull] Action<object> setResultValue)
            {
                Context = null;
                Fn = fn;
                Args = args;
                SetResultValue = setResultValue;
            }

            protected Descriptor(
                [CanBeNull] object context,
                [NotNull] Func<object[], IEnumerator> fn,
                [NotNull] object[] args,
                [CanBeNull] Action<object> setResultValue
            )
            {
                Context = context;
                Fn = fn;
                Args = args;
                SetResultValue = setResultValue;
            }

            [CanBeNull] public object Context { get; }
            [NotNull] public Func<object[], IEnumerator> Fn { get; }
            [NotNull] public object[] Args { get; }
            [CanBeNull] public Action<object> SetResultValue { get; }
        }
    }


    internal sealed class SingleReactiveProperty<T> : IObservable<T>
    {
        private IObserver<T>[] _data;
        private bool _isCalled;
        private T _value;
        private readonly object _lockObj = new object();

        /// <summary>
        /// Value
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public T Value
        {
            get => _value;
            set
            {
                IObserver<T>[] data;
                lock (_lockObj)
                {
                    if (_isCalled)
                    {
                        throw new InvalidOperationException();
                    }

                    data = _data;
                    _value = value;
                    _isCalled = true;
                }

                foreach (var observer in data)
                {
                    observer.OnNext(value);
                    observer.OnCompleted();
                }
            }
        }

        IDisposable IObservable<T>.Subscribe(IObserver<T> observer)
        {
            lock (_lockObj)
            {
                if (_isCalled)
                {
                    observer.OnNext(_value);
                    observer.OnCompleted();
                    return Disposable.Empty;
                }

                var newData = new IObserver<T>[_data.Length + 1];
                Array.Copy(_data, newData, _data.Length);
                newData[_data.Length] = observer;
                _data = newData;
            }

            return new Subscription(this, observer);
        }

        private class Subscription : IDisposable
        {
            private readonly object _lockObj = new object();
            private SingleReactiveProperty<T> _singleReactiveProperty;
            private IObserver<T> _observer;
            private bool _disposed;

            public Subscription(SingleReactiveProperty<T> singleReactiveProperty, IObserver<T> observer)
            {
                _singleReactiveProperty = singleReactiveProperty;
                _observer = observer;
            }

            public void Dispose()
            {
                lock (_lockObj)
                {
                    if (_disposed) return;
                    _disposed = true;
                    var singleReactiveProperty = _singleReactiveProperty;
                    var observer = _observer;
                    _singleReactiveProperty = null;
                    _observer = null;

                    lock (singleReactiveProperty._lockObj)
                    {
                        var data = singleReactiveProperty._data.ToList();
                        data.Remove(observer);
                        singleReactiveProperty._data = data.ToArray();
                    }
                }
            }
        }
    }

    internal class Disposable : IDisposable
    {
        public static readonly IDisposable Empty = new Disposable();

        private Disposable()
        {
        }

        public void Dispose()
        {
        }
    }
}
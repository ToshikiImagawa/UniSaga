// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections;
using System.Linq;
using JetBrains.Annotations;

namespace UniSaga.Core
{
    internal delegate IEnumerator InternalSaga(params object[] arguments);

    internal sealed class CombinatorEffectDescriptor
    {
        public CombinatorEffectDescriptor(IEffect[] effects)
        {
            Effects = effects;
        }

        public IEffect[] Effects { get; }
    }

    internal sealed class RaceEffect : IEffect
    {
        public RaceEffect(CombinatorEffectDescriptor payload)
        {
            EffectDescriptor = payload;
        }

        public CombinatorEffectDescriptor EffectDescriptor { get; }
    }

    internal sealed class AllEffect : IEffect
    {
        public AllEffect(CombinatorEffectDescriptor payload)
        {
            EffectDescriptor = payload;
        }

        public CombinatorEffectDescriptor EffectDescriptor { get; }
    }

    internal sealed class JoinEffect : IEffect
    {
        public JoinEffect(Descriptor payload)
        {
            EffectDescriptor = payload;
        }

        public Descriptor EffectDescriptor { get; }

        public class Descriptor
        {
            public Descriptor([NotNull] SagaCoroutine sagaCoroutine)
            {
                SagaCoroutine = sagaCoroutine;
            }

            [NotNull] public SagaCoroutine SagaCoroutine { get; }

            private static IEnumerator Empty()
            {
                yield break;
            }
        }
    }

    internal sealed class ForkEffect : IEffect
    {
        public ForkEffect([NotNull] Descriptor payload)
        {
            EffectDescriptor = payload;
        }

        public Descriptor EffectDescriptor { get; }

        public class Descriptor
        {
            public Descriptor(
                [NotNull] InternalSaga internalSaga,
                [NotNull] object[] args,
                [CanBeNull] Action<SagaCoroutine> setResultValue
            )
            {
                InternalSaga = internalSaga;
                Args = args;
                SetResultValue = setResultValue;
            }

            [NotNull] public InternalSaga InternalSaga { get; }
            [NotNull] public object[] Args { get; }
            [CanBeNull] public Action<SagaCoroutine> SetResultValue { get; }
        }
    }

    internal sealed class TakeEffect : IEffect
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

    internal sealed class PutEffect : IEffect
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

    internal sealed class SelectEffect : IEffect
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

    internal sealed class CancelEffect : IEffect
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

    internal sealed class CallEffect : IEffect
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
                [NotNull] object[] args
            )
            {
                Fn = fn;
                Args = args;
            }

            [NotNull] public Func<object[], IEnumerator> Fn { get; }
            [NotNull] public object[] Args { get; }
        }
    }

    internal sealed class SingleReactiveProperty<T> : IObservable<T>
    {
        private IObserver<T>[] _data = Array.Empty<IObserver<T>>();
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

        public IDisposable Subscribe(IObserver<T> observer)
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

    internal static class Enumerator
    {
        public static readonly IEnumerator Empty = _Empty();

        private static IEnumerator _Empty()
        {
            yield break;
        }
    }

    internal sealed class SimpleObserver<T> : IObserver<T>
    {
        private readonly Action _onCompleted;
        private readonly Action<Exception> _onError;
        private readonly Action<T> _onNext;
        private bool _completed;
        private readonly object _lockObj = new object();

        public SimpleObserver(
            Action<T> onNext = null,
            Action onCompleted = null,
            Action<Exception> onError = null
        )
        {
            _onCompleted = onCompleted;
            _onError = onError;
            _onNext = onNext;
        }

        public void OnCompleted()
        {
            Action onCompleted;
            lock (_lockObj)
            {
                if (_completed) return;
                _completed = true;
                onCompleted = _onCompleted;
            }

            onCompleted?.Invoke();
        }

        public void OnError(Exception error)
        {
            Action<Exception> onError;
            lock (_lockObj)
            {
                if (_completed) return;
                _completed = true;
                onError = _onError;
            }

            onError?.Invoke(error);
        }

        public void OnNext(T value)
        {
            Action<T> onNext;
            lock (_lockObj)
            {
                if (_completed) return;
                onNext = _onNext;
            }

            onNext?.Invoke(value);
        }
    }
}
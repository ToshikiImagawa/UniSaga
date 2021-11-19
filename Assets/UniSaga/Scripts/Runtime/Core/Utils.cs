// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UniSystem.Reactive.Disposables;

namespace UniSaga.Core
{
    internal static class Utils
    {
        public static IDisposable Subscribe<T>(
            this IObservable<T> self,
            Action<T> onNext
        )
        {
            return self.Subscribe(new ActionObserver<T>(onNext: onNext));
        }

        public static IDisposable Subscribe<T>(
            this IObservable<T> self,
            Action onCompleted
        )
        {
            return self.Subscribe(new ActionObserver<T>(onCompleted));
        }

        public static IDisposable Subscribe<T>(
            this IObservable<T> self,
            Action<T> onNext,
            Action onCompleted
        )
        {
            return self.Subscribe(new ActionObserver<T>(onCompleted, onNext: onNext));
        }

        public static IDisposable Subscribe<T>(
            this IObservable<T> self,
            Action<T> onNext,
            Action<Exception> onError
        )
        {
            return self.Subscribe(new ActionObserver<T>(
                onNext: onNext,
                onError: onError
            ));
        }

        public static IDisposable Subscribe<T>(
            this IObservable<T> self,
            Action<T> onNext,
            Action onCompleted,
            Action<Exception> onError
        )
        {
            return self.Subscribe(new ActionObserver<T>(onCompleted, onError, onNext));
        }
    }

    internal class ActionObserver<T> : IObserver<T>
    {
        [CanBeNull] private readonly Action _onCompleted;
        [CanBeNull] private readonly Action<Exception> _onError;
        [CanBeNull] private readonly Action<T> _onNext;

        public ActionObserver(
            [CanBeNull] Action onCompleted = null,
            [CanBeNull] Action<Exception> onError = null,
            [CanBeNull] Action<T> onNext = null)
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

    internal class SingleObservable<T> : IObservable<T>
    {
        private T _value;
        private readonly object _lock = new object();
        private readonly List<IObserver<T>> _observers = new List<IObserver<T>>();

        public void OnNext([NotNull] T exception)
        {
            IObserver<T>[] observers;
            lock (_lock)
            {
                if (_value != null) return;
                _value = exception;
                observers = _observers.ToArray();
                _observers.Clear();
            }

            foreach (var observer in observers)
            {
                observer.OnNext(_value);
                observer.OnCompleted();
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            lock (_lock)
            {
                if (_value == null)
                {
                    _observers.Add(observer);
                    return Disposable.Create(() => { _observers.Remove(observer); });
                }
            }

            observer.OnNext(_value);
            observer.OnCompleted();
            return Disposable.Empty;
        }
    }
}
// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections;
using JetBrains.Annotations;

namespace UniSaga.Plugin
{
    internal sealed class SagaCoroutine : IPlayerLoopItem
    {
        [NotNull] private readonly IEnumerator _enumerator;
        [CanBeNull] private readonly Action<Exception> _exceptionCallback;
        private bool _requestCancel;

        private SagaCoroutine(IEnumerator enumerator, Action<Exception> exceptionCallback)
        {
            _enumerator = InnerEnumerator(enumerator);
            _exceptionCallback = exceptionCallback;
        }

        private static IEnumerator InnerEnumerator(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                switch (current)
                {
                    case null:
                        yield return null;
                        break;
                    case IEnumerator e:
                        var ne = InnerEnumerator(e);
                        while (ne.MoveNext())
                        {
                            yield return null;
                        }

                        break;
                }
            }
        }

        public bool IsError { get; private set; }
        public bool IsCanceled { get; private set; }
        public bool IsCompleted { get; private set; }

        public Exception Error { get; private set; }

        public void RequestCancel()
        {
            _requestCancel = true;
        }

        public void SetError(Exception error)
        {
            IsError = true;
            Error = error;
            _exceptionCallback?.Invoke(error);
        }

        public bool MoveNext()
        {
            if (IsCompleted || IsCanceled || IsError) return false;
            if (_requestCancel)
            {
                IsCanceled = true;
                return false;
            }

            try
            {
                if (_enumerator.MoveNext()) return true;
            }
            catch (Exception error)
            {
                SetError(error);
                return false;
            }

            IsCompleted = true;
            return false;
        }

        public static SagaCoroutine StartCoroutine(
            [NotNull] IEnumerator enumerator,
            [CanBeNull] Action<Exception> exceptionCallback
        )
        {
            var coroutine = new SagaCoroutine(enumerator, exceptionCallback);
            PlayerLoopHelper.AddAction(coroutine);
            return coroutine;
        }
    }
}
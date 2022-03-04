// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace UniSaga.Core
{
    internal static class EffectHelper
    {
        public static IEnumerator<IEffect> TakeLatestHelper(
            [NotNull] object[] arguments,
            [CanBeNull] ReturnData<object> returnActionData
        )
        {
            if (arguments.Length < 2) throw new InvalidOperationException("Not enough arguments.");
            var pattern = arguments[0] as Func<object, bool> ?? throw new InvalidOperationException(
                $"The first argument must be one of type {nameof(Func<object, bool>)}. {arguments[0].GetType().FullName}");
            var worker = arguments[1] as InternalSaga ?? throw new InvalidOperationException(
                $"The second argument must be one of type {nameof(InternalSaga)}. {arguments[1].GetType().FullName}");

            var workerArgs = arguments.Length > 2 ? arguments.Skip(2).ToArray() : Array.Empty<object>();
            var forkCoroutine = new ReturnData<SagaCoroutine>();
            return FsmIterator(
                new Dictionary<string, Func<(string nextState, Func<IEffect> getEffect)>>
                {
                    {
                        "q1",
                        () => (nextState: "q2", getEffect: YTake)
                    },
                    {
                        "q2",
                        () =>
                        {
                            var runningCoroutine = forkCoroutine.Value;
                            return runningCoroutine?.IsCompleted ?? true
                                ? (nextState: "q1", getEffect: YFork)
                                : (nextState: "q3", getEffect: CreateYCancel(runningCoroutine));
                        }
                    },
                    {
                        "q3",
                        () => (nextState: "q1", getEffect: YFork)
                    }
                }, "q1"
            );

            IEffect YTake()
            {
                return Effects.Take(pattern, returnActionData);
            }

            IEffect YFork()
            {
                return Effects.Fork(worker, forkCoroutine, workerArgs);
            }

            Func<IEffect> CreateYCancel(SagaCoroutine coroutine)
            {
                return YCancel;

                IEffect YCancel()
                {
                    return Effects.Cancel(coroutine);
                }
            }
        }

        public static IEnumerator<IEffect> TakeEveryHelper(
            [NotNull] object[] arguments,
            [CanBeNull] ReturnData<object> returnActionData
        )
        {
            if (arguments.Length < 2) throw new InvalidOperationException("Not enough arguments.");
            var pattern = arguments[0] as Func<object, bool> ?? throw new InvalidOperationException(
                $"The first argument must be one of type {nameof(Func<object, bool>)}. {arguments[0].GetType().FullName}");
            var worker = arguments[1] as InternalSaga ?? throw new InvalidOperationException(
                $"The second argument must be one of type {nameof(InternalSaga)}. {arguments[1].GetType().FullName}");

            var workerArgs = arguments.Length > 2 ? arguments.Skip(2).ToArray() : Array.Empty<object>();
            return FsmIterator(
                new Dictionary<string, Func<(string nextState, Func<IEffect> getEffect)>>
                {
                    {
                        "q1",
                        () => (nextState: "q2", getEffect: YTake)
                    },
                    {
                        "q2",
                        () => (nextState: "q1", getEffect: YFork)
                    }
                }, "q1"
            );

            IEffect YTake()
            {
                return Effects.Take(pattern, returnActionData);
            }

            IEffect YFork()
            {
                return Effects.Fork(worker, null, workerArgs);
            }
        }

        private static IEnumerator<IEffect> FsmIterator(
            IReadOnlyDictionary<string, Func<(string nextState, Func<IEffect> getEffect)>> fsm,
            string startState
        )
        {
            var nextState = startState;
            while (nextState != null)
            {
                var currentState = fsm[nextState]();
                nextState = currentState.nextState;
                yield return currentState.getEffect();
            }
        }
    }
}
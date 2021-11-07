// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace UniSaga.Core
{
    internal static class EffectHelper
    {
        public static IEnumerator<IEffect> TakeLatestHelper(object[] arguments)
        {
            if (arguments.Length < 2) throw new InvalidOperationException();
            var pattern = arguments[0] as Func<object, bool> ?? throw new InvalidOperationException();
            var worker = arguments[1] as InternalSaga ?? throw new InvalidOperationException();

            var workerArgs = arguments.Length > 2 ? arguments.Skip(2).ToArray() : Array.Empty<object>();
            var forkTask = new ReturnData<SagaTask>();
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
                            var runningTask = forkTask.Value;
                            return runningTask?.IsCompleted ?? true
                                ? (nextState: "q1", getEffect: YFork)
                                : (nextState: "q3", getEffect: YCancel(runningTask));
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
                return Effects.Take(pattern);
            }

            IEffect YFork()
            {
                return Effects.Fork(worker, forkTask, workerArgs);
            }

            Func<IEffect> YCancel(SagaTask task)
            {
                return () => Effects.Cancel(task);
            }
        }

        public static IEnumerator<IEffect> TakeEveryHelper(object[] arguments)
        {
            if (arguments.Length < 2) throw new InvalidOperationException();
            var pattern = arguments[0] as Func<object, bool> ?? throw new InvalidOperationException();
            var worker = arguments[1] as InternalSaga ?? throw new InvalidOperationException();

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
                return TakeEffectCreator.Create(pattern);
            }

            IEffect YFork()
            {
                return ForkEffectCreator.Create(worker, null, workerArgs);
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
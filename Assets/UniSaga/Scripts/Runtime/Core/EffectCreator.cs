// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections;
using System.Linq;
using JetBrains.Annotations;

namespace UniSaga.Core
{
    internal static class CallEffectCreator
    {
        public static CallEffect Create(
            [NotNull] Func<SagaTask, IEnumerator> function
        )
        {
            return Create(
                (_, task) => function(task),
                Array.Empty<object>()
            );
        }

        public static CallEffect Create<TArgument>(
            [NotNull] Func<TArgument, SagaTask, IEnumerator> function,
            [CanBeNull] TArgument arg
        )
        {
            return Create(
                (p, task) => function(
                    (TArgument)p[0],
                    task
                ),
                new[]
                {
                    (object)arg
                }
            );
        }

        public static CallEffect Create<TArgument1, TArgument2>(
            [NotNull] Func<TArgument1, TArgument2, SagaTask, IEnumerator> function,
            [CanBeNull] TArgument1 arg1,
            [CanBeNull] TArgument2 arg2
        )
        {
            return Create(
                (p, task) => function(
                    (TArgument1)p[0],
                    (TArgument2)p[1],
                    task
                ),
                new[]
                {
                    (object)arg1,
                    arg2
                }
            );
        }

        public static CallEffect Create<TArgument1, TArgument2, TArgument3>(
            [NotNull] Func<TArgument1, TArgument2, TArgument3, SagaTask, IEnumerator> function,
            [CanBeNull] TArgument1 arg1,
            [CanBeNull] TArgument2 arg2,
            [CanBeNull] TArgument3 arg3
        )
        {
            return Create(
                (p, task) => function(
                    (TArgument1)p[0],
                    (TArgument2)p[1],
                    (TArgument3)p[2],
                    task
                ),
                new[]
                {
                    (object)arg1,
                    arg2,
                    arg3
                }
            );
        }

        private static CallEffect Create(
            [NotNull] Func<object[], SagaTask, IEnumerator> function,
            [NotNull] object[] args
        )
        {
            return new CallEffect(new CallEffect.Descriptor(
                p =>
                {
                    var sagaTask = (SagaTask)p.First();
                    var a = p.Skip(1).ToArray();
                    return function(a, sagaTask);
                },
                args,
                null
            ));
        }
    }

    internal static class CancelEffectCreator
    {
        public static CancelEffect Create([CanBeNull] SagaTask task)
        {
            return new CancelEffect(new CancelEffect.Descriptor(task));
        }
    }

    internal static class SelectEffectCreator<TState, TReturnData>
    {
        public static SelectEffect Create(
            [NotNull] Func<TState, TReturnData> selector,
            [NotNull] ReturnData<TReturnData> returnData
        )
        {
            return new SelectEffect(new SelectEffect.Descriptor(
                (state, param) => selector((TState)state),
                Array.Empty<object>(),
                obj =>
                {
                    if (obj == null)
                    {
                        returnData.SetValue(default);
                    }
                    else
                    {
                        if (!(obj is TReturnData value)) throw new InvalidOperationException();
                        returnData.SetValue(value);
                    }
                }
            ));
        }

        public static SelectEffect Create(
            [NotNull] Func<TState, object[], TReturnData> selector,
            [NotNull] ReturnData<TReturnData> returnData,
            [NotNull] params object[] args
        )
        {
            return new SelectEffect(new SelectEffect.Descriptor(
                (state, param) => selector((TState)state, param),
                args,
                obj =>
                {
                    if (obj == null)
                    {
                        returnData.SetValue(default);
                    }
                    else
                    {
                        if (!(obj is TReturnData value)) throw new InvalidOperationException();
                        returnData.SetValue(value);
                    }
                }
            ));
        }

        public static SelectEffect Create<TArgument>(
            [NotNull] Func<TState, TArgument, TReturnData> selector,
            [CanBeNull] TArgument arg,
            [NotNull] ReturnData<TReturnData> returnData
        )
        {
            return new SelectEffect(new SelectEffect.Descriptor(
                (state, param) => selector((TState)state, (TArgument)param[0]),
                new[] { (object)arg },
                obj =>
                {
                    if (obj == null)
                    {
                        returnData.SetValue(default);
                    }
                    else
                    {
                        if (!(obj is TReturnData value)) throw new InvalidOperationException();
                        returnData.SetValue(value);
                    }
                }
            ));
        }

        public static SelectEffect Create<TArgument1, TArgument2>(
            [NotNull] Func<TState, TArgument1, TArgument2, TReturnData> selector,
            [CanBeNull] TArgument1 arg1, [CanBeNull] TArgument2 arg2,
            [NotNull] ReturnData<TReturnData> returnData
        )
        {
            return new SelectEffect(new SelectEffect.Descriptor(
                (state, param) => selector(
                    (TState)state,
                    (TArgument1)param[0],
                    (TArgument2)param[1]
                ),
                new[]
                {
                    (object)arg1,
                    arg2
                },
                obj =>
                {
                    if (obj == null)
                    {
                        returnData.SetValue(default);
                    }
                    else
                    {
                        if (!(obj is TReturnData value)) throw new InvalidOperationException();
                        returnData.SetValue(value);
                    }
                }
            ));
        }

        public static SelectEffect Create<TArgument1, TArgument2, TArgument3>(
            [NotNull] Func<TState, TArgument1, TArgument2, TArgument3, TReturnData> selector,
            [CanBeNull] TArgument1 arg1, [CanBeNull] TArgument2 arg2, [CanBeNull] TArgument3 arg3,
            [NotNull] ReturnData<TReturnData> returnData
        )
        {
            return new SelectEffect(new SelectEffect.Descriptor(
                (state, param) => selector(
                    (TState)state,
                    (TArgument1)param[0],
                    (TArgument2)param[1],
                    (TArgument3)param[2]
                ),
                new[]
                {
                    (object)arg1,
                    arg2,
                    arg3
                },
                obj =>
                {
                    if (obj == null)
                    {
                        returnData.SetValue(default);
                    }
                    else
                    {
                        if (!(obj is TReturnData value)) throw new InvalidOperationException();
                        returnData.SetValue(value);
                    }
                }
            ));
        }
    }

    internal static class PutEffectCreator
    {
        public static PutEffect Create(object action)
        {
            return new PutEffect(new PutEffect.Descriptor(action));
        }
    }

    internal static class TakeEffectCreator
    {
        public static TakeEffect Create(
            [NotNull] Func<object, bool> pattern
        )
        {
            return new TakeEffect(new TakeEffect.Descriptor(pattern));
        }
    }

    internal static class ForkEffectCreator
    {
        public static ForkEffect Create(
            [NotNull] InternalSaga saga,
            [CanBeNull] ReturnData<SagaTask> returnData,
            [NotNull] object[] arguments
        )
        {
            Action<object> setResultValue = null;
            if (returnData != null)
            {
                setResultValue = obj =>
                {
                    if (obj == null)
                    {
                        returnData.SetValue(default);
                    }
                    else
                    {
                        if (!(obj is SagaTask value)) throw new InvalidOperationException();
                        returnData.SetValue(value);
                    }
                };
            }

            return new ForkEffect(new ForkEffect.Descriptor(
                saga,
                arguments,
                setResultValue
            ));
        }
    }

    internal static class JoinEffectCreator
    {
        public static JoinEffect Create([NotNull] SagaTask sagaTask)
        {
            return new JoinEffect(new JoinEffect.Descriptor(sagaTask));
        }
    }

    internal static class AllEffectCreator
    {
        public static AllEffect Create([NotNull] IEffect[] effects)
        {
            return new AllEffect(new CombinatorEffectDescriptor(effects));
        }
    }

    internal static class RaceEffectCreator
    {
        public static RaceEffect Create([NotNull] IEffect[] effects)
        {
            return new RaceEffect(new CombinatorEffectDescriptor(effects));
        }
    }
}
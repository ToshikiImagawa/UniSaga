// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace UniSaga.Core
{
    internal static class CallEffectCreator
    {
        private static readonly object Unit = new object();

        public static CallEffect Create(
            [NotNull] Func<CancellationToken, UniTask> function
        )
        {
            return new CallEffect(new CallEffect.CallEffectDescriptor(
                async (p, token) =>
                {
                    await function(token);
                    return Unit;
                },
                Array.Empty<object>(),
                null
            ));
        }

        public static CallEffect Create(
            [NotNull] Func<object[], CancellationToken, UniTask> function,
            [NotNull] object[] args
        )
        {
            return new CallEffect(new CallEffect.CallEffectDescriptor(
                async (p, token) =>
                {
                    await function(p, token);
                    return Unit;
                },
                args,
                null
            ));
        }

        public static CallEffect Create<TArgument>(
            Func<TArgument, CancellationToken, UniTask> function,
            TArgument arg
        )
        {
            return new CallEffect(new CallEffect.CallEffectDescriptor(
                async (p, token) =>
                {
                    await function((TArgument)p[0], token);
                    return Unit;
                },
                new[] { (object)arg },
                null
            ));
        }
    }

    internal static class CallEffectCreator<TReturnData>
    {
        public static CallEffect Create(
            [NotNull] Func<object[], CancellationToken, UniTask<TReturnData>> function,
            [NotNull] object[] args,
            [CanBeNull] ReturnData<TReturnData> returnData
        )
        {
            if (function == null) throw new InvalidOperationException();
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
                        if (!(obj is TReturnData value)) throw new InvalidOperationException();
                        returnData.SetValue(value);
                    }
                };
            }

            return new CallEffect(new CallEffect.CallEffectDescriptor(
                async (p, token) => await function(p, token),
                args,
                setResultValue
            ));
        }

        public static CallEffect Create(
            [NotNull] Func<CancellationToken, UniTask<TReturnData>> function,
            [CanBeNull] ReturnData<TReturnData> returnData)
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
                        if (!(obj is TReturnData value)) throw new InvalidOperationException();
                        returnData.SetValue(value);
                    }
                };
            }

            return new CallEffect(new CallEffect.CallEffectDescriptor(
                async (p, token) => await function(token),
                Array.Empty<object>(),
                setResultValue
            ));
        }

        public static CallEffect Create<TArgument>(
            [NotNull] Func<TArgument, CancellationToken, UniTask<TReturnData>> function,
            [CanBeNull] TArgument arg,
            [CanBeNull] ReturnData<TReturnData> returnData
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
                        if (!(obj is TReturnData value)) throw new InvalidOperationException();
                        returnData.SetValue(value);
                    }
                };
            }

            return new CallEffect(new CallEffect.CallEffectDescriptor(
                async (p, token) => await function(
                    (TArgument)p[0],
                    token
                ),
                new[] { (object)arg },
                setResultValue
            ));
        }

        public static CallEffect Create<TArgument1, TArgument2>(
            [NotNull] Func<TArgument1, TArgument2, CancellationToken, UniTask<TReturnData>> function,
            [CanBeNull] TArgument1 arg1, [CanBeNull] TArgument2 arg2,
            [CanBeNull] ReturnData<TReturnData> returnData
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
                        if (!(obj is TReturnData value)) throw new InvalidOperationException();
                        returnData.SetValue(value);
                    }
                };
            }

            return new CallEffect(new CallEffect.CallEffectDescriptor(
                async (p, token) => await function(
                    (TArgument1)p[0],
                    (TArgument2)p[1],
                    token
                ), new[]
                {
                    (object)arg1,
                    arg2
                },
                setResultValue)
            );
        }

        public static CallEffect Create<TArgument1, TArgument2, TArgument3>(
            [NotNull] Func<TArgument1, TArgument2, TArgument3, CancellationToken, UniTask<TReturnData>> function,
            [CanBeNull] TArgument1 arg1, [CanBeNull] TArgument2 arg2, [CanBeNull] TArgument3 arg3,
            [CanBeNull] ReturnData<TReturnData> returnData
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
                        if (!(obj is TReturnData value)) throw new InvalidOperationException();
                        returnData.SetValue(value);
                    }
                };
            }

            return new CallEffect(new CallEffect.CallEffectDescriptor(
                async (p, token) => await function(
                    (TArgument1)p[0],
                    (TArgument2)p[1],
                    (TArgument3)p[2],
                    token
                ), new[]
                {
                    (object)arg1,
                    arg2,
                    arg3
                },
                setResultValue)
            );
        }
    }

    internal static class CancelEffectCreator
    {
        public static CancelEffect Create([CanBeNull] SagaTask task)
        {
            return new CancelEffect(new CancelEffect.CancelEffectDescriptor(task));
        }
    }

    internal static class SelectEffectCreator<TState, TReturnData>
    {
        public static SelectEffect Create(
            [NotNull] Func<TState, TReturnData> selector,
            [NotNull] ReturnData<TReturnData> returnData
        )
        {
            return new SelectEffect(new SelectEffect.SelectEffectDescriptor(
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
            return new SelectEffect(new SelectEffect.SelectEffectDescriptor(
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
            return new SelectEffect(new SelectEffect.SelectEffectDescriptor(
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
            return new SelectEffect(new SelectEffect.SelectEffectDescriptor(
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
            return new SelectEffect(new SelectEffect.SelectEffectDescriptor(
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
            return new PutEffect(new PutEffect.PutEffectDescriptor(action));
        }
    }

    internal static class TakeEffectCreator
    {
        public static TakeEffect Create(
            [NotNull] Func<object, bool> pattern
        )
        {
            return new TakeEffect(new TakeEffect.TakeEffectDescriptor(pattern));
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

            return new ForkEffect(new ForkEffect.ForkEffectDescriptor(
                saga,
                arguments,
                setResultValue,
                true
            ));
        }
    }

    internal static class JoinEffectCreator
    {
        public static JoinEffect Create([NotNull] SagaTask sagaTask)
        {
            return new JoinEffect(new JoinEffect.JoinEffectDescriptor(sagaTask));
        }
    }

    internal static class AllEffectCreator
    {
        public static AllEffect Create([NotNull] IEffect[] effects)
        {
            return new AllEffect(new AllEffect.AllEffectDescriptor(effects));
        }
    }

    internal static class RaceEffectCreator
    {
        public static RaceEffect Create([NotNull] IEffect[] effects)
        {
            return new RaceEffect(new RaceEffect.RaceEffectDescriptor(effects));
        }
    }
}
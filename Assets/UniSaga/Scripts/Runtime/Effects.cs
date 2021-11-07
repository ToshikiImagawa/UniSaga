// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniSaga.Core;

namespace UniSaga
{
    public static class Effects
    {
        #region Call

        public static IEffect Call(
            Func<object[], CancellationToken, UniTask> function,
            params object[] args
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            if (args == null) throw new InvalidOperationException(nameof(args));
            return CallEffectCreator.Create(function, args);
        }

        public static IEffect Call(
            Func<CancellationToken, UniTask> function
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            return CallEffectCreator.Create(function);
        }

        public static IEffect Call<TArgument>(
            Func<TArgument, CancellationToken, UniTask> function,
            TArgument arg
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            return CallEffectCreator.Create(function, arg);
        }

        public static IEffect Call<TReturnData>(
            Func<object[], CancellationToken, UniTask<TReturnData>> function,
            ReturnData<TReturnData> returnData = null,
            params object[] args
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            if (args == null) throw new InvalidOperationException(nameof(args));
            return CallEffectCreator<TReturnData>.Create(function, args, returnData);
        }

        public static IEffect Call<TReturnData>(
            Func<CancellationToken, UniTask<TReturnData>> function,
            ReturnData<TReturnData> returnData = null
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            return CallEffectCreator<TReturnData>.Create(function, returnData);
        }

        public static IEffect Call<TArgument, TReturnData>(
            Func<TArgument, CancellationToken, UniTask<TReturnData>> function,
            TArgument arg,
            ReturnData<TReturnData> returnData = null
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            return CallEffectCreator<TReturnData>.Create(function, arg, returnData);
        }

        public static IEffect Call<TArgument1, TArgument2, TReturnData>(
            Func<TArgument1, TArgument2, CancellationToken, UniTask<TReturnData>> function,
            TArgument1 arg1, TArgument2 arg2,
            ReturnData<TReturnData> returnData = null
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            return CallEffectCreator<TReturnData>.Create(function, arg1, arg2, returnData);
        }

        public static IEffect Call<TArgument1, TArgument2, TArgument3, TReturnData>(
            Func<TArgument1, TArgument2, TArgument3, CancellationToken, UniTask<TReturnData>> function,
            TArgument1 arg1, TArgument2 arg2, TArgument3 arg3,
            ReturnData<TReturnData> returnData = null
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            return CallEffectCreator<TReturnData>.Create(function, arg1, arg2, arg3, returnData);
        }

        #endregion

        #region Cancel

        public static IEffect Cancel(SagaTask task = null)
        {
            return CancelEffectCreator.Create(task);
        }

        #endregion

        #region Select

        public static IEffect Select<TState, TReturnData>(
            Func<TState, object[], TReturnData> selector,
            ReturnData<TReturnData> returnData,
            params object[] args
        )
        {
            if (selector == null) throw new InvalidOperationException(nameof(selector));
            if (returnData == null) throw new InvalidOperationException(nameof(returnData));
            if (args == null) throw new InvalidOperationException(nameof(args));
            return SelectEffectCreator<TState, TReturnData>.Create(selector, returnData, args);
        }

        public static IEffect Select<TState, TReturnData>(
            Func<TState, TReturnData> selector,
            ReturnData<TReturnData> returnData
        )
        {
            if (selector == null) throw new InvalidOperationException(nameof(selector));
            if (returnData == null) throw new InvalidOperationException(nameof(returnData));
            return SelectEffectCreator<TState, TReturnData>.Create(selector, returnData);
        }

        public static IEffect Select<TState, TArgument, TReturnData>(
            Func<TState, TArgument, TReturnData> selector,
            TArgument arg,
            ReturnData<TReturnData> returnData
        )
        {
            if (selector == null) throw new InvalidOperationException(nameof(selector));
            if (returnData == null) throw new InvalidOperationException(nameof(returnData));
            return SelectEffectCreator<TState, TReturnData>.Create(selector, arg, returnData);
        }

        public static IEffect Select<TState, TArgument1, TArgument2, TReturnData>(
            Func<TState, TArgument1, TArgument2, TReturnData> selector,
            TArgument1 arg1, TArgument2 arg2,
            ReturnData<TReturnData> returnData
        )
        {
            if (selector == null) throw new InvalidOperationException(nameof(selector));
            if (returnData == null) throw new InvalidOperationException(nameof(returnData));
            return SelectEffectCreator<TState, TReturnData>.Create(selector, arg1, arg2, returnData);
        }

        public static IEffect Select<TState, TArgument1, TArgument2, TArgument3, TReturnData>(
            Func<TState, TArgument1, TArgument2, TArgument3, TReturnData> selector,
            TArgument1 arg1, TArgument2 arg2, TArgument3 arg3,
            ReturnData<TReturnData> returnData
        )
        {
            if (selector == null) throw new InvalidOperationException(nameof(selector));
            if (returnData == null) throw new InvalidOperationException(nameof(returnData));
            return SelectEffectCreator<TState, TReturnData>.Create(selector, arg1, arg2, arg3, returnData);
        }

        #endregion

        #region Put

        public static IEffect Put(object action)
        {
            if (action == null) throw new InvalidOperationException(nameof(action));
            return PutEffectCreator.Create(action);
        }

        #endregion

        #region Take

        public static IEffect Take(
            Func<object, bool> pattern
        )
        {
            if (pattern == null) throw new InvalidOperationException(nameof(pattern));
            return TakeEffectCreator.Create(pattern);
        }

        #endregion

        #region Fork

        public static IEffect Fork(Saga saga, ReturnData<SagaTask> returnData = null)
        {
            return Fork(_ => saga(), returnData, Array.Empty<object>());
        }

        public static IEffect Fork<TArgument>(
            Saga<TArgument> saga,
            TArgument argument,
            ReturnData<SagaTask> returnData = null
        )
        {
            return Fork(args => saga((TArgument)args[0]), returnData, argument);
        }

        public static IEffect Fork<TArgument1, TArgument2>(
            Saga<TArgument1, TArgument2> saga,
            TArgument1 argument1,
            TArgument2 argument2,
            ReturnData<SagaTask> returnData = null
        )
        {
            return Fork(
                args => saga(
                    (TArgument1)args[0],
                    (TArgument2)args[1]
                ),
                returnData,
                argument1,
                argument2
            );
        }

        public static IEffect Fork<TArgument1, TArgument2, TArgument3>(
            Saga<TArgument1, TArgument2, TArgument3> saga,
            TArgument1 argument1,
            TArgument2 argument2,
            TArgument3 argument3,
            ReturnData<SagaTask> returnData = null
        )
        {
            return Fork(
                args => saga(
                    (TArgument1)args[0],
                    (TArgument2)args[1],
                    (TArgument3)args[2]
                ),
                returnData,
                argument1,
                argument2,
                argument3
            );
        }

        internal static IEffect Fork(
            InternalSaga saga,
            ReturnData<SagaTask> returnData = null,
            params object[] arguments)
        {
            if (saga == null) throw new InvalidOperationException(nameof(saga));
            return ForkEffectCreator.Create(saga, returnData, arguments ?? Array.Empty<object>());
        }

        #endregion

        #region Join

        public static IEffect Join(SagaTask sagaTask)
        {
            if (sagaTask == null) throw new InvalidOperationException(nameof(sagaTask));
            return JoinEffectCreator.Create(sagaTask);
        }

        #endregion

        #region TakeEvery

        public static IEffect TakeEvery(
            Func<object, bool> pattern,
            Saga worker,
            ReturnData<SagaTask> returnData = null
        )
        {
            return TakeEvery(
                pattern,
                _ => worker(),
                returnData,
                Array.Empty<object>()
            );
        }

        public static IEffect TakeEvery<TArgument>(
            Func<object, bool> pattern,
            Saga<TArgument> worker,
            TArgument argument,
            ReturnData<SagaTask> returnData = null
        )
        {
            return TakeEvery(
                pattern,
                args => worker(
                    (TArgument)args[0]
                ),
                returnData,
                argument
            );
        }

        public static IEffect TakeEvery<TArgument1, TArgument2>(
            Func<object, bool> pattern,
            Saga<TArgument1, TArgument2> worker,
            TArgument1 argument1,
            TArgument2 argument2,
            ReturnData<SagaTask> returnData = null
        )
        {
            return TakeEvery(
                pattern,
                args => worker(
                    (TArgument1)args[0],
                    (TArgument2)args[1]
                ),
                returnData,
                argument1,
                argument2
            );
        }

        public static IEffect TakeEvery<TArgument1, TArgument2, TArgument3>(
            Func<object, bool> pattern,
            Saga<TArgument1, TArgument2, TArgument3> worker,
            TArgument1 argument1,
            TArgument2 argument2,
            TArgument3 argument3,
            ReturnData<SagaTask> returnData = null
        )
        {
            return TakeEvery(
                pattern,
                args => worker(
                    (TArgument1)args[0],
                    (TArgument2)args[1],
                    (TArgument3)args[2]
                ),
                returnData,
                argument1,
                argument2,
                argument3
            );
        }

        private static IEffect TakeEvery(
            Func<object, bool> pattern,
            InternalSaga worker,
            ReturnData<SagaTask> returnData = null,
            params object[] arguments
        )
        {
            if (pattern == null) throw new InvalidOperationException(nameof(pattern));
            if (worker == null) throw new InvalidOperationException(nameof(worker));

            var innerArgs = new[] { (object)pattern, worker };
            if (arguments != null && arguments.Length > 0)
            {
                innerArgs = innerArgs.Concat(arguments).ToArray();
            }

            return ForkEffectCreator.Create(
                EffectHelper.TakeEveryHelper,
                returnData,
                innerArgs
            );
        }

        #endregion

        #region TakeLatest

        public static IEffect TakeLatest(
            Func<object, bool> pattern,
            Saga worker,
            ReturnData<SagaTask> returnData = null
        )
        {
            return TakeLatest(
                pattern,
                _ => worker(),
                returnData,
                Array.Empty<object>()
            );
        }

        public static IEffect TakeLatest<TArgument>(
            Func<object, bool> pattern,
            Saga<TArgument> worker,
            TArgument argument,
            ReturnData<SagaTask> returnData = null
        )
        {
            return TakeLatest(
                pattern,
                args => worker(
                    (TArgument)args[0]
                ),
                returnData,
                argument
            );
        }

        public static IEffect TakeLatest<TArgument1, TArgument2>(
            Func<object, bool> pattern,
            Saga<TArgument1, TArgument2> worker,
            TArgument1 argument1,
            TArgument2 argument2,
            ReturnData<SagaTask> returnData = null
        )
        {
            return TakeLatest(
                pattern,
                args => worker(
                    (TArgument1)args[0],
                    (TArgument2)args[1]
                ),
                returnData,
                argument1,
                argument2
            );
        }

        public static IEffect TakeLatest<TArgument1, TArgument2, TArgument3>(
            Func<object, bool> pattern,
            Saga<TArgument1, TArgument2, TArgument3> worker,
            TArgument1 argument1,
            TArgument2 argument2,
            TArgument3 argument3,
            ReturnData<SagaTask> returnData = null
        )
        {
            return TakeLatest(
                pattern,
                args => worker(
                    (TArgument1)args[0],
                    (TArgument2)args[1],
                    (TArgument3)args[2]
                ),
                returnData,
                argument1,
                argument2,
                argument3
            );
        }

        private static IEffect TakeLatest(
            Func<object, bool> pattern,
            InternalSaga worker,
            ReturnData<SagaTask> returnData = null,
            params object[] arguments
        )
        {
            if (pattern == null) throw new InvalidOperationException(nameof(pattern));
            if (worker == null) throw new InvalidOperationException(nameof(worker));

            var innerArgs = new[] { (object)pattern, worker };
            if (arguments != null && arguments.Length > 0)
            {
                innerArgs = innerArgs.Concat(arguments).ToArray();
            }

            return ForkEffectCreator.Create(
                EffectHelper.TakeLatestHelper,
                returnData,
                innerArgs
            );
        }

        #endregion

        #region All

        public static IEffect All(params IEffect[] effects)
        {
            if (effects == null) throw new InvalidOperationException(nameof(effects));
            return AllEffectCreator.Create(effects);
        }

        #endregion

        #region Race

        public static IEffect Race(params IEffect[] effects)
        {
            if (effects == null) throw new InvalidOperationException(nameof(effects));
            return RaceEffectCreator.Create(effects);
        }

        #endregion

        #region Delay

        public static IEffect Delay(int millisecondsDelay)
        {
            return Call(async token => { await UniTask.Delay(millisecondsDelay, cancellationToken: token); });
        }

        public static IEffect DelayFrame(int delayFrameCount)
        {
            return Call(async token => { await UniTask.DelayFrame(delayFrameCount, cancellationToken: token); });
        }

        #endregion
    }
}
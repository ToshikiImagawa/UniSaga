// Copyright @2021 COMCREATE. All rights reserved.

using System;
using Cysharp.Threading.Tasks;
using UniSaga.Core;

namespace UniSaga
{
    public static class Effects
    {
        #region Call

        public static IEffect Call<TReturnData>(
            Func<object[], UniTask<TReturnData>> function,
            ReturnData<TReturnData> returnData = null,
            params object[] args
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            if (args == null) throw new InvalidOperationException(nameof(args));
            return CallEffectCreator<TReturnData>.Create(function, args, returnData);
        }

        public static IEffect Call<TReturnData>(
            Func<UniTask<TReturnData>> function,
            ReturnData<TReturnData> returnData = null
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            return CallEffectCreator<TReturnData>.Create(function, returnData);
        }

        public static IEffect Call<TArgument, TReturnData>(
            Func<TArgument, UniTask<TReturnData>> function,
            TArgument arg,
            ReturnData<TReturnData> returnData = null
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            return CallEffectCreator<TReturnData>.Create(function, arg, returnData);
        }

        public static IEffect Call<TArgument1, TArgument2, TReturnData>(
            Func<TArgument1, TArgument2, UniTask<TReturnData>> function,
            TArgument1 arg1, TArgument2 arg2,
            ReturnData<TReturnData> returnData = null
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            return CallEffectCreator<TReturnData>.Create(function, arg1, arg2, returnData);
        }

        public static IEffect Call<TArgument1, TArgument2, TArgument3, TReturnData>(
            Func<TArgument1, TArgument2, TArgument3, UniTask<TReturnData>> function,
            TArgument1 arg1, TArgument2 arg2, TArgument3 arg3,
            ReturnData<TReturnData> returnData = null
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            return CallEffectCreator<TReturnData>.Create(function, arg1, arg2, arg3, returnData);
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

        public static IEffect Take(Func<object, bool> pattern)
        {
            if (pattern == null) throw new InvalidOperationException(nameof(pattern));
            return TakeEffectCreator.Create(pattern);
        }

        #endregion

        #region Fork

        public static IEffect Fork(Saga saga)
        {
            if (saga == null) throw new InvalidOperationException(nameof(saga));
            return ForkEffectCreator.Create(saga);
        }

        #endregion
    }
}
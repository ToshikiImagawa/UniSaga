// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniSaga.Threading.Core;

namespace UniSaga.Threading
{
    public static class AsyncEffects
    {
        #region Call

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

        public static IEffect Call<TArgument1, TArgument2>(
            Func<TArgument1, TArgument2, CancellationToken, UniTask> function,
            TArgument1 arg1, TArgument2 arg2
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            return CallEffectCreator.Create(function, arg1, arg2);
        }

        public static IEffect Call<TArgument1, TArgument2, TArgument3>(
            Func<TArgument1, TArgument2, TArgument3, CancellationToken, UniTask> function,
            TArgument1 arg1, TArgument2 arg2, TArgument3 arg3
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            return CallEffectCreator.Create(function, arg1, arg2, arg3);
        }

        public static IEffect Call<TReturnData>(
            Func<CancellationToken, UniTask<TReturnData>> function,
            out ReturnData<TReturnData> returnData
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            returnData = new ReturnData<TReturnData>();
            return CallEffectCreator<TReturnData>.Create(function, returnData);
        }

        public static IEffect Call<TArgument, TReturnData>(
            Func<TArgument, CancellationToken, UniTask<TReturnData>> function,
            TArgument arg,
            out ReturnData<TReturnData> returnData
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            returnData = new ReturnData<TReturnData>();
            return CallEffectCreator<TReturnData>.Create(function, arg, returnData);
        }

        public static IEffect Call<TArgument1, TArgument2, TReturnData>(
            Func<TArgument1, TArgument2, CancellationToken, UniTask<TReturnData>> function,
            TArgument1 arg1, TArgument2 arg2,
            out ReturnData<TReturnData> returnData
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            returnData = new ReturnData<TReturnData>();
            return CallEffectCreator<TReturnData>.Create(function, arg1, arg2, returnData);
        }

        public static IEffect Call<TArgument1, TArgument2, TArgument3, TReturnData>(
            Func<TArgument1, TArgument2, TArgument3, CancellationToken, UniTask<TReturnData>> function,
            TArgument1 arg1, TArgument2 arg2, TArgument3 arg3,
            out ReturnData<TReturnData> returnData
        )
        {
            if (function == null) throw new InvalidOperationException(nameof(function));
            returnData = new ReturnData<TReturnData>();
            return CallEffectCreator<TReturnData>.Create(function, arg1, arg2, arg3, returnData);
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
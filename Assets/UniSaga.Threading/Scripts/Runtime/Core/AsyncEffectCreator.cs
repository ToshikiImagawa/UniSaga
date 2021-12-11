// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UniSaga.Core;

namespace UniSaga.Threading.Core
{
    internal static class CallEffectCreator
    {
        public static CallEffect Create(
            [NotNull] Func<CancellationToken, UniTask> function
        )
        {
            return Create(
                (_, token) => function(token),
                Array.Empty<object>()
            );
        }

        public static CallEffect Create<TArgument>(
            [NotNull] Func<TArgument, CancellationToken, UniTask> function,
            [CanBeNull] TArgument arg
        )
        {
            return Create(
                (p, token) => function(
                    (TArgument)p[0],
                    token
                ),
                new[]
                {
                    (object)arg
                }
            );
        }

        public static CallEffect Create<TArgument1, TArgument2>(
            [NotNull] Func<TArgument1, TArgument2, CancellationToken, UniTask> function,
            [CanBeNull] TArgument1 arg1,
            [CanBeNull] TArgument2 arg2
        )
        {
            return Create(
                (p, token) => function(
                    (TArgument1)p[0],
                    (TArgument2)p[1],
                    token
                ),
                new[]
                {
                    (object)arg1,
                    arg2
                }
            );
        }

        public static CallEffect Create<TArgument1, TArgument2, TArgument3>(
            [NotNull] Func<TArgument1, TArgument2, TArgument3, CancellationToken, UniTask> function,
            [CanBeNull] TArgument1 arg1,
            [CanBeNull] TArgument2 arg2,
            [CanBeNull] TArgument3 arg3
        )
        {
            return Create(
                (p, token) => function(
                    (TArgument1)p[0],
                    (TArgument2)p[1],
                    (TArgument3)p[2],
                    token
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
            [NotNull] Func<object[], CancellationToken, UniTask> function,
            [NotNull] object[] args
        )
        {
            return new CallEffect(new CallEffect.Descriptor(
                p =>
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    var sagaCoroutine = (SagaCoroutine)p.First();
                    var a = p.Skip(1).ToArray();
                    sagaCoroutine.OnCanceled.Subscribe(
                        new CancellationObserver<UniRedux.VoidMessage>(cancellationTokenSource));
                    return InnerTask(function(a, cancellationTokenSource.Token), sagaCoroutine).ToCoroutine();
                },
                args,
                null
            ));

            static async UniTask InnerTask(UniTask uniTask, SagaCoroutine sagaCoroutine)
            {
                try
                {
                    await uniTask;
                }
                catch (Exception error)
                {
                    sagaCoroutine.SetError(error);
                }
            }
        }
    }

    internal static class CallEffectCreator<TReturnData>
    {
        public static CallEffect Create(
            [NotNull] Func<CancellationToken, UniTask<TReturnData>> function,
            [CanBeNull] ReturnData<TReturnData> returnData
        )
        {
            return Create(
                (p, token) => function(token),
                Array.Empty<object>(),
                returnData
            );
        }

        public static CallEffect Create<TArgument>(
            [NotNull] Func<TArgument, CancellationToken, UniTask<TReturnData>> function,
            [CanBeNull] TArgument arg,
            [CanBeNull] ReturnData<TReturnData> returnData
        )
        {
            return Create(
                (p, token) => function(
                    (TArgument)p[0],
                    token
                ),
                new[]
                {
                    (object)arg
                },
                returnData
            );
        }

        public static CallEffect Create<TArgument1, TArgument2>(
            [NotNull] Func<TArgument1, TArgument2, CancellationToken, UniTask<TReturnData>> function,
            [CanBeNull] TArgument1 arg1,
            [CanBeNull] TArgument2 arg2,
            [CanBeNull] ReturnData<TReturnData> returnData
        )
        {
            return Create(
                (p, token) => function(
                    (TArgument1)p[0],
                    (TArgument2)p[1],
                    token
                ),
                new[]
                {
                    (object)arg1,
                    arg2,
                },
                returnData
            );
        }

        public static CallEffect Create<TArgument1, TArgument2, TArgument3>(
            [NotNull] Func<TArgument1, TArgument2, TArgument3, CancellationToken, UniTask<TReturnData>> function,
            [CanBeNull] TArgument1 arg1,
            [CanBeNull] TArgument2 arg2,
            [CanBeNull] TArgument3 arg3,
            [CanBeNull] ReturnData<TReturnData> returnData
        )
        {
            return Create(
                (p, token) => function(
                    (TArgument1)p[0],
                    (TArgument2)p[1],
                    (TArgument3)p[2],
                    token
                ),
                new[]
                {
                    (object)arg1,
                    arg2,
                    arg3
                },
                returnData
            );
        }

        private static CallEffect Create(
            [NotNull] Func<object[], CancellationToken, UniTask<TReturnData>> function,
            [NotNull] object[] args,
            [CanBeNull] ReturnData<TReturnData> returnData
        )
        {
            return new CallEffect(new CallEffect.Descriptor(
                p =>
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    var sagaCoroutine = (SagaCoroutine)p.First();
                    var a = p.Skip(1).ToArray();
                    sagaCoroutine.OnCanceled.Subscribe(
                        new CancellationObserver<UniRedux.VoidMessage>(cancellationTokenSource));
                    return InnerTask(function(a, cancellationTokenSource.Token), returnData, sagaCoroutine)
                        .ToCoroutine();
                },
                args,
                null
            ));

            static async UniTask InnerTask(
                UniTask<TReturnData> task,
                ReturnData<TReturnData> returnData,
                SagaCoroutine sagaCoroutine
            )
            {
                try
                {
                    returnData?.SetValue(await task);
                }
                catch (Exception error)
                {
                    sagaCoroutine.SetError(error);
                }
            }
        }
    }
}
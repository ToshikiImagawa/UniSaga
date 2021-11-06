// Copyright @2021 COMCREATE. All rights reserved.

using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniSaga.Sample.Repository
{
    public static class UserRepository
    {
        public static async UniTask<int> GetUserId(CancellationToken token)
        {
            await UniTask.Delay(1000, cancellationToken: token);
            return 5;
        }
    }
}
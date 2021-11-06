// Copyright @2021 COMCREATE. All rights reserved.

using System.Threading;
using Cysharp.Threading.Tasks;
using UniSaga.Sample.Repository;

namespace UniSaga.Sample.Services
{
    public static class Api
    {
        public static async UniTask<(bool, int)> GetUserIdTask(
            int currentUserId,
            CancellationToken token
        )
        {
            var userId = await UserRepository.GetUserId(token);

            if (currentUserId > 0 && currentUserId != userId)
            {
                return (true, 0);
            }

            return (false, userId);
        }
    }
}
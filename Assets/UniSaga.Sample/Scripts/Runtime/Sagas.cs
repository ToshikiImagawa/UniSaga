// Copyright @2021 COMCREATE. All rights reserved.

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniSaga.Sample.Services;
using UnityEngine;

namespace UniSaga.Sample
{
    public static class Sagas
    {
        public static IEnumerator<IEffect> RootSaga()
        {
            // 現在のUserIdが5か判定
            {
                var checkUserId = CheckUserId5Saga();
                while (checkUserId.MoveNext()) yield return checkUserId.Current;
            }

            // UserIdの取得
            {
                var getUser = GetUserSaga();
                while (getUser.MoveNext()) yield return getUser.Current;
            }

            // 現在のUserIdが5か判定
            {
                var checkUserId = CheckUserId5Saga();
                while (checkUserId.MoveNext()) yield return checkUserId.Current;
            }
            Debug.Log("Wait RestartAction");
            yield return Effects.Take(action => action is RestartAction);
            Debug.Log("Run RestartAction");
            Debug.Log("Before ForkSaga");
            var forkTask = new ReturnData<SagaTask>();
            yield return Effects.Fork(ForkSaga, forkTask);
            Debug.Log("After ForkSaga");
            yield return Effects.Call(async token =>
            {
                Debug.Log("Wait 2s");
                await UniTask.Delay(2000, cancellationToken: token);
            });
            yield return Effects.Put(new RestartAction());
            Debug.Log("Wait for ForkTask to complete");
            yield return Effects.Join(forkTask.Value);
            Debug.Log("ForkTask is complete");

            // UserIdの取得
            {
                var getUser = GetUserSaga();
                while (getUser.MoveNext()) yield return getUser.Current;
            }

            // 現在のUserIdが5か判定
            {
                var checkUserId = CheckUserId5Saga();
                while (checkUserId.MoveNext()) yield return checkUserId.Current;
            }
        }

        private static IEnumerator<IEffect> CheckUserId5Saga()
        {
            var selectReturnData = new ReturnData<(bool, int)>();
            yield return Effects.Select<SampleState, int, (bool, int)>(
                (state, currentId) => (state.Id == currentId, state.Id),
                5,
                selectReturnData
            );
            var (is5, id) = selectReturnData.Value;
            Debug.Log(is5 ? "ID is 5" : $"ID is not 5 : {id}");
        }

        private static IEnumerator<IEffect> GetUserSaga()
        {
            var currentUserIdReturnData = new ReturnData<int>();
            yield return Effects.Select<SampleState, int>(
                state => state.Id,
                currentUserIdReturnData
            );
            Debug.Log("GetUserIdTask start");
            var getTaskReturnData = new ReturnData<(bool, int)>();
            yield return Effects.Call(Api.GetUserIdTask,
                currentUserIdReturnData.Value,
                getTaskReturnData
            );
            var (isError, id) = getTaskReturnData.Value;
            if (isError)
            {
                Debug.Log("GetUserIdTask error");
                yield return Effects.Put(new ErrorAction("GetUserIdTask error"));
            }
            else
            {
                Debug.Log("GetUserIdTask complete");
                yield return Effects.Put(new SetIdAction(id));
            }
        }

        private static IEnumerator<IEffect> ForkSaga()
        {
            Debug.Log("Start ForkSaga");
            yield return Effects.Take(action => action is RestartAction);
            yield return Effects.Call(async token =>
            {
                Debug.Log("Wait 2s");
                await UniTask.Delay(2000, cancellationToken: token);
            });
            Debug.Log("End ForkSaga");
        }
    }
}
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
            const string logPrefix = "<color=green>[RootSaga]</color>";

            var takeEveryTask = new ReturnData<SagaTask>();
            yield return Effects.TakeEvery(
                action => action is StartAction,
                TakeEverySaga,
                takeEveryTask
            );
            var takeLatestTask = new ReturnData<SagaTask>();
            yield return Effects.TakeLatest(
                action => action is StartAction,
                TakeLatestSaga,
                takeLatestTask
            );

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
            Debug.Log($"{logPrefix} Wait RestartAction");
            yield return Effects.Take(action => action is RestartAction);
            Debug.Log($"{logPrefix} Run RestartAction");
            Debug.Log($"{logPrefix} Before ForkSaga");
            var forkTask = new ReturnData<SagaTask>();
            yield return Effects.Fork(ForkSaga, forkTask);
            Debug.Log($"{logPrefix} After ForkSaga");
            yield return Effects.Call(async token =>
            {
                Debug.Log($"{logPrefix} Wait 2s");
                await UniTask.Delay(2000, cancellationToken: token);
            });
            Debug.Log($"{logPrefix} Put RestartAction");
            yield return Effects.Put(new RestartAction());
            Debug.Log($"{logPrefix} Wait for ForkTask to complete");
            yield return Effects.Join(forkTask.Value);
            Debug.Log($"{logPrefix} ForkTask is complete");

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

            Debug.Log($"{logPrefix} Put StartAction 1");
            yield return Effects.Put(new StartAction());

            Debug.Log($"{logPrefix} Put StartAction 2");
            yield return Effects.Put(new StartAction());
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
            const string logPrefix = "<color=blue>[ForkSaga]</color>";
            Debug.Log($"{logPrefix} Start ForkSaga");
            Debug.Log($"{logPrefix} Take RestartAction");
            yield return Effects.Take(action => action is RestartAction);
            Debug.Log($"{logPrefix} Restart ForkSaga");
            yield return Effects.Call(async token =>
            {
                Debug.Log($"{logPrefix} Wait 2s");
                await UniTask.Delay(2000, cancellationToken: token);
            });
            Debug.Log($"{logPrefix} End ForkSaga");
        }

        private static int _takeEverySagaIndex;

        private static IEnumerator<IEffect> TakeEverySaga()
        {
            var logPrefix = $"<color=red>[ForkSaga:{_takeEverySagaIndex}]</color>";
            _takeEverySagaIndex++;
            Debug.Log($"{logPrefix} Start TakeEverySaga");

            yield return Effects.Call(async token =>
            {
                Debug.Log($"{logPrefix} Wait 2s");
                await UniTask.Delay(2000, cancellationToken: token);
            });
            Debug.Log($"{logPrefix} End ForkSaga");
        }

        private static IEnumerator<IEffect> TakeLatestSaga()
        {
            var logPrefix = $"<color=red>[ForkSaga:{_takeEverySagaIndex}]</color>";
            _takeEverySagaIndex++;
            Debug.Log($"{logPrefix} Start TakeLatestSaga");

            yield return Effects.Call(async token =>
            {
                Debug.Log($"{logPrefix} Wait 2s");
                await UniTask.Delay(2000, cancellationToken: token);
            });
            Debug.Log($"{logPrefix} End ForkSaga");
        }
    }
}
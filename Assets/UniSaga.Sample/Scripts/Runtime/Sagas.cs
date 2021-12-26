// Copyright @2021 COMCREATE. All rights reserved.

using System.Collections;
using Cysharp.Threading.Tasks;
using UniSaga.Sample.Services;
using UniSaga.Threading;
using UnityEngine;

namespace UniSaga.Sample
{
    public static class Sagas
    {
        public static IEnumerator RootSaga()
        {
            const string logPrefix = "<color=green>[RootSaga]</color>";

            var takeEveryCoroutine = new ReturnData<SagaCoroutine>();
            yield return Effects.TakeEvery(
                action => action is StartAction,
                TakeEverySaga,
                takeEveryCoroutine
            );
            var takeLatestCoroutine = new ReturnData<SagaCoroutine>();
            yield return Effects.TakeLatest(
                action => action is StartAction,
                TakeLatestSaga,
                takeLatestCoroutine
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
            Debug.Log($"{logPrefix} Wait {nameof(RestartAction)}");
            yield return Effects.Take(action => action is RestartAction);
            Debug.Log($"{logPrefix} Fork {nameof(ForkSaga)}");
            var forkCoroutine = new ReturnData<SagaCoroutine>();
            yield return Effects.Fork(ForkSaga, forkCoroutine);
            Debug.Log($"{logPrefix} Wait 2s");
            yield return AsyncEffects.Delay(2000);
            Debug.Log($"{logPrefix} Put {nameof(RestartAction)}");
            yield return Effects.Put(new RestartAction());
            Debug.Log($"{logPrefix} Wait for {nameof(forkCoroutine)} to complete");
            yield return Effects.Join(forkCoroutine.Value);
            Debug.Log($"{logPrefix} {nameof(forkCoroutine)} is completed");

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

        private static IEnumerator CheckUserId5Saga()
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

        private static IEnumerator GetUserSaga()
        {
            var currentUserIdReturnData = new ReturnData<int>();
            yield return Effects.Select<SampleState, int>(
                state => state.Id,
                currentUserIdReturnData
            );
            Debug.Log("GetUserIdTask start");
            yield return AsyncEffects.Call(
                Api.GetUserIdTask,
                currentUserIdReturnData.Value,
                out var getTaskReturnData
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

        private static IEnumerator ForkSaga()
        {
            const string logPrefix = "<color=blue>[ForkSaga]</color>";
            Debug.Log($"{logPrefix} Start ForkSaga");
            Debug.Log($"{logPrefix} Take RestartAction");
            yield return Effects.Take(action => action is RestartAction);
            Debug.Log($"{logPrefix} Restart ForkSaga");
            Debug.Log($"{logPrefix} Wait 2s");
            yield return Effects.Delay(2000);
            Debug.Log($"{logPrefix} End ForkSaga");
        }

        private static int _takeEverySagaIndex;

        private static IEnumerator TakeEverySaga()
        {
            var logPrefix = $"<color=red>[ForkSaga:{_takeEverySagaIndex}]</color>";
            _takeEverySagaIndex++;
            Debug.Log($"{logPrefix} Start TakeEverySaga");

            Debug.Log($"{logPrefix} Wait 1 frame");
            yield return AsyncEffects.DelayFrame(1);
            Debug.Log($"{logPrefix} End 1 frame");

            yield return Effects.Race(
                AsyncEffects.Call(async token =>
                {
                    Debug.Log($"{logPrefix} Race Call 1 Wait 2s");
                    await UniTask.Delay(2000, cancellationToken: token);
                    Debug.Log($"{logPrefix} Race Call 1 End");
                }),
                AsyncEffects.Call(async token =>
                {
                    Debug.Log($"{logPrefix} Race Call 2 Wait 4s");
                    await UniTask.Delay(4000, cancellationToken: token);
                    Debug.Log($"{logPrefix} Race Call 2 End");
                })
            );
            Debug.Log($"{logPrefix} End ForkSaga");
        }

        private static IEnumerator TakeLatestSaga()
        {
            var logPrefix = $"<color=red>[ForkSaga:{_takeEverySagaIndex}]</color>";
            _takeEverySagaIndex++;
            Debug.Log($"{logPrefix} Start TakeLatestSaga");

            Debug.Log($"{logPrefix} Wait 1 frame");
            yield return Effects.DelayFrame(1);
            Debug.Log($"{logPrefix} End 1 frame");

            yield return Effects.All(
                AsyncEffects.Call(async token =>
                {
                    Debug.Log($"{logPrefix} All Call 1 Wait 2s");
                    await UniTask.Delay(2000, cancellationToken: token);
                    Debug.Log($"{logPrefix} All Call 1 End");
                }),
                AsyncEffects.Call(async token =>
                {
                    Debug.Log($"{logPrefix} All Call 2 Wait 4s");
                    await UniTask.Delay(4000, cancellationToken: token);
                    Debug.Log($"{logPrefix} All Call 2 End");
                })
            );
            Debug.Log($"{logPrefix} End ForkSaga");
        }
    }
}
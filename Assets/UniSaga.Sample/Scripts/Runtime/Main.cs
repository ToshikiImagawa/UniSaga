// Copyright @2021 COMCREATE. All rights reserved.

using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UniSaga.Sample
{
    public class Main : MonoBehaviour
    {
        public async void Awake()
        {
            var uniSagaMiddleware = new UniSagaMiddleware<SampleState>();
            var store = UniRedux.Redux.CreateStore(
                SampleReducer.Execute,
                new SampleState(1),
                uniSagaMiddleware.Middleware
            );
            uniSagaMiddleware.Run(RootSaga);
            store.Dispatch(new SetIdAction(4));
            await UniTask.Delay(20000);
            store.Dispatch(new RestartAction());
        }

        private static IEnumerator<IEffect> RootSaga()
        {
            var selectReturnData = new ReturnData<(bool, int)>();
            yield return Effects.Select<SampleState, int, (bool, int)>((state, id) => (state.Id == id, state.Id), 5,
                selectReturnData);
            {
                var (is5, id) = selectReturnData.Value;
                Debug.Log(is5 ? "IDが5" : $"IDが5以外 : {id}");
            }
            Debug.Log("開始");

            var callReturnData = new ReturnData<(bool, int)>();
            yield return Effects.Call(ApiGet, "https://www.comcreate-info.com#getUser",
                new Dictionary<string, string>(), callReturnData);
            {
                var (isError, id) = callReturnData.Value;
                if (isError)
                {
                    Debug.Log("通信失敗");
                }
                else
                {
                    Debug.Log("通信成功");
                    yield return Effects.Put(new SetIdAction(id));
                }
            }

            yield return Effects.Select<SampleState, int, (bool, int)>((state, id) => (state.Id == id, state.Id), 5,
                selectReturnData);
            {
                var (is5, id) = selectReturnData.Value;
                Debug.Log(is5 ? "IDが5" : $"IDが5以外 : {id}");
            }
            Debug.Log("Wait RestartAction");
            yield return Effects.Take(action => action is RestartAction);
            Debug.Log("Run RestartAction");
            Debug.Log("Before ForkSaga");
            yield return Effects.Fork(ForkSaga);
            Debug.Log("After ForkSaga");
            Debug.Log("Wait 2s");
            yield return Effects.Call(Wait, 2);
            yield return Effects.Put(new RestartAction());
        }

        private static async UniTask<(bool, int)> ApiGet(string path, IDictionary<string, string> header)
        {
            Debug.Log($"通信中 {path}, {header}");
            await UniTask.Delay(10000);
            return (false, 5);
        }

        private static async UniTask<int> Wait(int second)
        {
            await UniTask.Delay(second * 1000);
            return second;
        }

        private static IEnumerator<IEffect> ForkSaga()
        {
            Debug.Log("Start ForkSaga");
            yield return Effects.Take(action => action is RestartAction);
            Debug.Log("End ForkSaga");
        }
    }

    public class SampleState
    {
        public SampleState(int id)
        {
            Id = id;
        }

        public int Id { get; }
    }

    public static class SampleReducer
    {
        public static SampleState Execute(SampleState previousState, object action)
        {
            switch (action)
            {
                case SetIdAction setAction:
                    return new SampleState(setAction.Id);
                default:
                    return previousState;
            }
        }
    }

    public readonly struct SetIdAction
    {
        public SetIdAction(int id)
        {
            Id = id;
        }

        public int Id { get; }
    }

    public readonly struct RestartAction
    {
    }
}
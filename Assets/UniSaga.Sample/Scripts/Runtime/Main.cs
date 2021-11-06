// Copyright @2021 COMCREATE. All rights reserved.

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UniSaga.Sample
{
    public class Main : MonoBehaviour
    {
        public async void Awake()
        {
            var store = Store.ConfigureStore();
            await UniTask.Delay(2000);
            store.Dispatch(new SetIdAction(4));
            store.Dispatch(new RestartAction());
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
}
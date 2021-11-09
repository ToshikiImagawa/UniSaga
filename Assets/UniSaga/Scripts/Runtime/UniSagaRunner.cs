// Copyright @2021 COMCREATE. All rights reserved.

using System.Collections;
using UnityEngine;

namespace UniSaga
{
    public class UniSagaRunner : MonoBehaviour
    {
        private static UniSagaRunner _instance;
        private static readonly object Lock = new object();

        internal static UniSagaRunner Instance
        {
            get
            {
                lock (Lock)
                {
                    if (_instance != null && _instance.gameObject != null) return _instance;
                    _instance = new GameObject("[UniSagaRunner]").AddComponent<UniSagaRunner>();
                    DontDestroyOnLoad(_instance.gameObject);
                    return _instance;
                }
            }
        }

        internal Coroutine UniSagaStartCoroutine(IEnumerator enumerator)
        {
            return StartCoroutine(enumerator);
        }

        internal void UniSagaStopCoroutine(Coroutine coroutine)
        {
            StopCoroutine(coroutine);
        }
    }
}
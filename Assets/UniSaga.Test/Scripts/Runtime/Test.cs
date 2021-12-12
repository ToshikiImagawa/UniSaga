// Copyright @2021 COMCREATE. All rights reserved.

using NUnit.Framework;
using UnityEngine;

namespace UniSaga.Test
{
    public class Test
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("Setup");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("Teardown");
        }

        // [UnityTest]
        // public IEnumerator AsyncTest() => UniTask.ToCoroutine(async () =>
        // {
        //     Debug.Log("Start");
        //     await UniTask.Delay(2000);
        //     Debug.Log("2s");
        //     await UniTask.Delay(2000);
        //     Debug.Log("4s");
        //     await UniTask.Delay(2000);
        //     Debug.Log("6s");
        // });
    }
}
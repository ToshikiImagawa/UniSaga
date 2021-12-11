// Copyright @2021 COMCREATE. All rights reserved.

using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniSaga.Test.Core
{
    public class TypesTest
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
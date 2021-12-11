// Copyright @2021 COMCREATE. All rights reserved.

using System;
using NSubstitute;
using NUnit.Framework;
using UniSaga.Core;
using UnityEngine;

namespace UniSaga.Test.Core
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class ObservableExtensionsTest
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

            // [Test]
            // public void SingleObservable_Success()
            // {
            //     // setup
            //     var intSingleObservable = new SingleObservable<int>();
            //     var stringSingleObservable = new SingleObservable<string>();
            //     var intObserverMock = Substitute.For<IObserver<int>>();
            //     var stringObserverMock = Substitute.For<IObserver<string>>();
            //     // execute
            //     using (intSingleObservable.Subscribe(intObserverMock))
            //     using (stringSingleObservable.Subscribe(stringObserverMock))
            //     {
            //         intSingleObservable.Execute(5);
            //         stringSingleObservable.Execute("test");
            //         // verify
            //         intObserverMock.Received(1).OnNext(5);
            //         intObserverMock.Received(1).OnCompleted();
            //         intObserverMock.Received(0).OnError(Arg.Any<Exception>());
            //         stringObserverMock.Received(1).OnNext("test");
            //         stringObserverMock.Received(1).OnCompleted();
            //         stringObserverMock.Received(0).OnError(Arg.Any<Exception>());
            //     }
            // }
            //
            // [Test]
            // public void SingleObservable_重複実行()
            // {
            //     // setup
            //     var intSingleObservable = new SingleObservable<int>();
            //     var stringSingleObservable = new SingleObservable<string>();
            //     var intObserverMock = Substitute.For<IObserver<int>>();
            //     var stringObserverMock = Substitute.For<IObserver<string>>();
            //     // execute
            //     using (intSingleObservable.Subscribe(intObserverMock))
            //     using (stringSingleObservable.Subscribe(stringObserverMock))
            //     {
            //         intSingleObservable.Execute(5);
            //         stringSingleObservable.Execute("test");
            //         intSingleObservable.Execute(4);
            //         stringSingleObservable.Execute("test2");
            //         // verify
            //         intObserverMock.Received(0).OnNext(4);
            //         intObserverMock.Received(1).OnCompleted();
            //         stringObserverMock.Received(0).OnNext("test2");
            //         stringObserverMock.Received(1).OnCompleted();
            //     }
            // }
            //
            // [Test]
            // public void SingleObservable_遅延Subscribe()
            // {
            //     // setup
            //     var intSingleObservable = new SingleObservable<int>();
            //     var stringSingleObservable = new SingleObservable<string>();
            //     var intObserverMock = Substitute.For<IObserver<int>>();
            //     var stringObserverMock = Substitute.For<IObserver<string>>();
            //     // execute
            //     intSingleObservable.Execute(5);
            //     stringSingleObservable.Execute("test");
            //     using (intSingleObservable.Subscribe(intObserverMock))
            //     using (stringSingleObservable.Subscribe(stringObserverMock))
            //     {
            //         // verify
            //         intObserverMock.Received(1).OnNext(5);
            //         intObserverMock.Received(1).OnCompleted();
            //         stringObserverMock.Received(1).OnNext("test");
            //         stringObserverMock.Received(1).OnCompleted();
            //     }
            // }
            //
            // [Test]
            // public void SingleObservable_遅延SubscribeError()
            // {
            //     // setup
            //     var intSingleObservable = new SingleObservable<int>();
            //     var stringSingleObservable = new SingleObservable<string>();
            //     var intObserverMock = Substitute.For<IObserver<int>>();
            //     var stringObserverMock = Substitute.For<IObserver<string>>();
            //     // execute
            //     using (intSingleObservable.Subscribe(intObserverMock))
            //     using (stringSingleObservable.Subscribe(stringObserverMock))
            //     {
            //     }
            //
            //     intSingleObservable.Execute(5);
            //     stringSingleObservable.Execute("test");
            //     // verify
            //     intObserverMock.Received(0).OnNext(5);
            //     intObserverMock.Received(0).OnCompleted();
            //     stringObserverMock.Received(0).OnNext("test");
            //     stringObserverMock.Received(0).OnCompleted();
            // }
        }
    }
}
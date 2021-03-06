// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using NUnit.Framework;
using UniSaga.Core;

namespace UniSaga.Test.Core
{
    [ExcludeFromCodeCoverage]
    public class TypesTest
    {
        private IObserver<int> _intObserverMock = Substitute.For<IObserver<int>>();
        private IObserver<string> _stringObserverMock = Substitute.For<IObserver<string>>();

        [SetUp]
        public void Setup()
        {
            _intObserverMock = Substitute.For<IObserver<int>>();
            _stringObserverMock = Substitute.For<IObserver<string>>();
        }

        [TearDown]
        public void Teardown()
        {
            _intObserverMock = null;
            _stringObserverMock = null;
        }

        [Test]
        public void SetValue()
        {
            // setup
            var intReactiveProperty = new SingleReactiveProperty<int>();
            var stringReactiveProperty = new SingleReactiveProperty<string>();
            using (intReactiveProperty.Subscribe(_intObserverMock))
            using (stringReactiveProperty.Subscribe(_stringObserverMock))
            {
                // execute
                intReactiveProperty.Value = 10;
                stringReactiveProperty.Value = "test01";
                // verify
                _intObserverMock.Received(1).OnNext(10);
                _intObserverMock.Received(1).OnCompleted();
                _intObserverMock.Received(0).OnError(Arg.Any<Exception>());
                _stringObserverMock.Received(1).OnNext("test01");
                _stringObserverMock.Received(1).OnCompleted();
                _stringObserverMock.Received(0).OnError(Arg.Any<Exception>());
            }
        }

        [Test]
        public void 遅延Subscribe()
        {
            // setup
            var intReactiveProperty = new SingleReactiveProperty<int>();
            var stringReactiveProperty = new SingleReactiveProperty<string>();
            intReactiveProperty.Value = 10;
            stringReactiveProperty.Value = "test01";
            // execute
            using (intReactiveProperty.Subscribe(_intObserverMock))
            using (stringReactiveProperty.Subscribe(_stringObserverMock))
            {
                // verify
                _intObserverMock.Received(1).OnNext(10);
                _intObserverMock.Received(1).OnCompleted();
                _intObserverMock.Received(0).OnError(Arg.Any<Exception>());
                _stringObserverMock.Received(1).OnNext("test01");
                _stringObserverMock.Received(1).OnCompleted();
                _stringObserverMock.Received(0).OnError(Arg.Any<Exception>());
            }
        }

        [Test]
        public void Dispose()
        {
            // setup
            var intReactiveProperty = new SingleReactiveProperty<int>();
            var stringReactiveProperty = new SingleReactiveProperty<string>();
            var intDisposable = intReactiveProperty.Subscribe(_intObserverMock);
            var stringDisposable = stringReactiveProperty.Subscribe(_stringObserverMock);
            intDisposable.Dispose();
            stringDisposable.Dispose();
            // execute
            intReactiveProperty.Value = 10;
            stringReactiveProperty.Value = "test01";
            // verify
            _intObserverMock.Received(0).OnNext(Arg.Any<int>());
            _stringObserverMock.Received(0).OnNext(Arg.Any<string>());
            intDisposable.Dispose();
            stringDisposable.Dispose();
        }

        [Test]
        public void 重複実行エラー()
        {
            // setup
            var intReactiveProperty = new SingleReactiveProperty<int>();
            var stringReactiveProperty = new SingleReactiveProperty<string>();
            intReactiveProperty.Value = 10;
            stringReactiveProperty.Value = "test01";

            // execute
            void IntReactivePropertyTestDelegate()
            {
                intReactiveProperty.Value = 20;
            }

            void StringReactivePropertyTestDelegate()
            {
                stringReactiveProperty.Value = "test02";
            }

            var actualInt = intReactiveProperty.Value;
            var actualString = stringReactiveProperty.Value;

            // verify
            Assert.Throws<InvalidOperationException>(IntReactivePropertyTestDelegate);
            Assert.Throws<InvalidOperationException>(StringReactivePropertyTestDelegate);
            using (intReactiveProperty.Subscribe(_intObserverMock))
            using (stringReactiveProperty.Subscribe(_stringObserverMock))
            {
                _intObserverMock.Received(1).OnNext(10);
                _intObserverMock.Received(1).OnCompleted();
                _intObserverMock.Received(0).OnError(Arg.Any<Exception>());
                _stringObserverMock.Received(1).OnNext("test01");
                _stringObserverMock.Received(1).OnCompleted();
                _stringObserverMock.Received(0).OnError(Arg.Any<Exception>());
            }

            Assert.AreEqual(10, actualInt);
            Assert.AreEqual("test01", actualString);
        }
    }
}
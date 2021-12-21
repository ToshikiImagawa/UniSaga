// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using NUnit.Framework;
using UniSaga.Core;
using UnityEngine;

namespace UniSaga.Test.Core
{
    [ExcludeFromCodeCoverage]
    public class EffectRunnerTest
    {
        private Func<MockState> _getStateMock;
        private Func<object, object> _dispatchMock;
        private IObservable<object> _subjectMock;

        [SetUp]
        public void Setup()
        {
            _getStateMock = Substitute.For<Func<MockState>>();
            _dispatchMock = Substitute.For<Func<object, object>>();
            _subjectMock = Substitute.For<IObservable<object>>();
        }

        [TearDown]
        public void Teardown()
        {
            _getStateMock = null;
            _dispatchMock = null;
            _subjectMock = null;
        }

        [Test]
        public void AllEffect()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );

            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var sagaCoroutineMock2 = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var sagaCoroutineMock3 = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            sagaCoroutineMock.StartCoroutine(Arg.Do<IEnumerator>(enumerator => enumerator.TestRun()))
                .ReturnsForAnyArgs(sagaCoroutineMock2, sagaCoroutineMock3);
            sagaCoroutineMock2.IsCompleted.Returns(true);
            sagaCoroutineMock3.IsCompleted.Returns(true);

            var effectMock1 = Substitute.For<IEffect>();
            var effectMock2 = Substitute.For<IEffect>();
            var allEffect = Effects.All(effectMock1, effectMock2);
            // execute
            effectRunner.RunEffect(allEffect, sagaCoroutineMock).TestRun();
            // verify
            sagaCoroutineMock2.Received().RequestCancel();
            sagaCoroutineMock3.Received().RequestCancel();
        }

        [Test]
        public void AllEffect_Wait()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );

            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var sagaCoroutineMock2 = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var sagaCoroutineMock3 = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            sagaCoroutineMock.StartCoroutine(Arg.Any<IEnumerator>())
                .ReturnsForAnyArgs(sagaCoroutineMock2, sagaCoroutineMock3);
            sagaCoroutineMock2.IsCompleted.Returns(true);
            sagaCoroutineMock3.IsCompleted.Returns(false);

            var effectMock1 = Substitute.For<IEffect>();
            var effectMock2 = Substitute.For<IEffect>();
            var allEffect = Effects.All(effectMock1, effectMock2);

            // execute
            void TestDelegate()
            {
                effectRunner.RunEffect(allEffect, sagaCoroutineMock).TestRun();
            }

            // verify
            var ex = Assert.Throws<Exception>(TestDelegate);
            Assert.AreEqual("無限リストになっています", ex.Message);
        }

        [Test]
        public void RaceEffect()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );

            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var sagaCoroutineMock2 = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var sagaCoroutineMock3 = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            sagaCoroutineMock.StartCoroutine(Arg.Any<IEnumerator>())
                .ReturnsForAnyArgs(sagaCoroutineMock2, sagaCoroutineMock3);
            sagaCoroutineMock2.IsCompleted.Returns(false);
            sagaCoroutineMock3.IsCompleted.Returns(true);

            var effectMock1 = Substitute.For<IEffect>();
            var effectMock2 = Substitute.For<IEffect>();
            var effect = Effects.Race(effectMock1, effectMock2);
            // execute
            effectRunner.RunEffect(effect, sagaCoroutineMock).TestRun();
            // verify
            sagaCoroutineMock2.Received().RequestCancel();
            sagaCoroutineMock3.Received().RequestCancel();
        }

        [Test]
        public void RaceEffect_Wait()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );

            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var sagaCoroutineMock2 = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var sagaCoroutineMock3 = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            sagaCoroutineMock.StartCoroutine(Arg.Any<IEnumerator>())
                .ReturnsForAnyArgs(sagaCoroutineMock2, sagaCoroutineMock3);
            sagaCoroutineMock2.IsCompleted.Returns(false);
            sagaCoroutineMock3.IsCompleted.Returns(false);

            var effectMock1 = Substitute.For<IEffect>();
            var effectMock2 = Substitute.For<IEffect>();
            var effect = Effects.Race(effectMock1, effectMock2);

            // execute
            void TestDelegate()
            {
                effectRunner.RunEffect(effect, sagaCoroutineMock).TestRun();
            }

            // verify
            var ex = Assert.Throws<Exception>(TestDelegate);
            Assert.AreEqual("無限リストになっています", ex.Message);
        }

        [Test]
        public void CallEffect()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );

            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var fnMock = Substitute.For<Func<int, string, SagaCoroutine, IEnumerator>>();
            fnMock.Invoke(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<SagaCoroutine>()).Returns(Enumerator.Empty);
            var callEffect = Effects.Call(fnMock, 1, "test");
            // execute
            effectRunner.RunEffect(callEffect, sagaCoroutineMock).TestRun();
            // verify
            fnMock
                .Received()
                .Invoke(
                    Arg.Do<int>(
                        id => { Assert.AreEqual(1, id); }
                    ),
                    Arg.Do<string>(
                        message => { Assert.AreEqual("test", message); }
                    ),
                    Arg.Do<SagaCoroutine>(
                        coroutine => { Assert.AreEqual(sagaCoroutineMock, coroutine); }
                    )
                );
        }

        [Test]
        public void CancelEffect()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );

            var sagaCoroutineMock1 = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var sagaCoroutineMock2 = Substitute.For<SagaCoroutine>(sagaCoroutineMock1, Enumerator.Empty);
            var callEffect = Effects.Cancel(sagaCoroutineMock2);
            // execute
            effectRunner.RunEffect(callEffect, sagaCoroutineMock1).TestRun();
            // verify
            sagaCoroutineMock1.Received(0).RequestCancel();
            sagaCoroutineMock2.Received().RequestCancel();
        }

        [Test]
        public void CancelEffect_Null()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );

            var sagaCoroutineMock1 = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var sagaCoroutineMock2 = Substitute.For<SagaCoroutine>(sagaCoroutineMock1, Enumerator.Empty);
            var callEffect = Effects.Cancel();
            // execute
            effectRunner.RunEffect(callEffect, sagaCoroutineMock2).TestRun();
            // verify
            sagaCoroutineMock1.Received(0).RequestCancel();
            sagaCoroutineMock2.Received().RequestCancel();
        }

        [Test]
        public void SelectEffect()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );

            var state = new MockState();
            var args = new object[] { 1, "2", 3, "4" };
            const string returnValue = "test";
            _getStateMock().Returns(state);
            var selectorMock = Substitute.For<Func<MockState, object[], string>>();
            selectorMock(state, args).Returns(returnValue);
            var setResultValue = new ReturnData<string>();
            var effect = Effects.Select(selectorMock, setResultValue, args);
            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            // execute
            effectRunner.RunEffect(effect, sagaCoroutineMock).TestRun();
            // verify
            Assert.AreEqual(returnValue, setResultValue.Value);
        }

        [Test]
        public void PutEffect()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );

            var action = new MockAction();
            var putEffect = Effects.Put(action);
            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            // execute
            effectRunner.RunEffect(putEffect, sagaCoroutineMock).TestRun();
            // verify
            _dispatchMock.Received()(action);
        }

        [Test]
        public void TakeEffect()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );

            var action = new MockAction();
            var effect = Effects.Take(a => a == action);
            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            IObserver<object> observer = null;
            _subjectMock
                .Subscribe(Arg.Do<IObserver<object>>(o => { observer = o; }))
                .Returns(Disposable.Empty);
            // execute
            var e = effectRunner.RunEffect(effect, sagaCoroutineMock);
            observer?.OnNext(action);
            // verify
            e.TestRun();
        }

        [Test]
        public void TakeEffect_Error()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );

            var action = new MockAction();
            var effect = Effects.Take(a => false);
            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            IObserver<object> observer = null;
            _subjectMock
                .Subscribe(Arg.Do<IObserver<object>>(o => { observer = o; }))
                .Returns(Disposable.Empty);
            // execute
            var e = effectRunner.RunEffect(effect, sagaCoroutineMock);
            observer?.OnNext(action);

            // verify
            void TestDelegate()
            {
                e.TestRun();
            }

            var ex = Assert.Throws<Exception>(TestDelegate);
            Assert.AreEqual("無限リストになっています", ex.Message);
        }

        [Test]
        public void ForkEffect()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );
            var args = new object[] { 1, "2", 3, "4" };
            var returnData = new ReturnData<SagaCoroutine>();
            var internalSagaMock = Substitute.For<Saga<object[]>>();
            var effect = Effects.Fork(internalSagaMock, args, returnData);
            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var sagaCoroutineMock2 = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            sagaCoroutineMock.StartCoroutine(Arg.Any<IEnumerator>()).Returns(sagaCoroutineMock2);
            // execute
            effectRunner.RunEffect(effect, sagaCoroutineMock);
            // verify
            internalSagaMock.Received()(args);
            Assert.AreEqual(sagaCoroutineMock2, returnData.Value);
        }

        [Test]
        public void JoinEffect()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );
            var waitSagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var effect = Effects.Join(waitSagaCoroutineMock);
            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            waitSagaCoroutineMock.IsCompleted.Returns(true);
            // execute & verify
            effectRunner.RunEffect(effect, sagaCoroutineMock).TestRun();
        }

        [Test]
        public void JoinEffect_Canceled()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );
            var waitSagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var effect = Effects.Join(waitSagaCoroutineMock);
            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            waitSagaCoroutineMock.IsCanceled.Returns(true);
            // execute & verify
            effectRunner.RunEffect(effect, sagaCoroutineMock).TestRun();
        }

        [Test]
        public void JoinEffect_Error()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );
            var waitSagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var effect = Effects.Join(waitSagaCoroutineMock);
            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            waitSagaCoroutineMock.IsError.Returns(true);
            // execute & verify
            effectRunner.RunEffect(effect, sagaCoroutineMock).TestRun();
        }

        [Test]
        public void JoinEffect_Wait()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );
            var waitSagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var effect = Effects.Join(waitSagaCoroutineMock);
            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            // execute
            var e = effectRunner.RunEffect(effect, sagaCoroutineMock);

            // verify
            void TestDelegate()
            {
                e.TestRun();
            }

            var ex = Assert.Throws<Exception>(TestDelegate);
            Assert.AreEqual("無限リストになっています", ex.Message);
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        [ExcludeFromCodeCoverage]
        private sealed class MockState
        {
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        [ExcludeFromCodeCoverage]
        private sealed class MockAction
        {
        }
    }
}
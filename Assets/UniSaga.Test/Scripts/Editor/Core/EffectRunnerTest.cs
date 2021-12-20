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
            var descriptor = new CombinatorEffectDescriptor(new[] { effectMock1, effectMock2 });
            var allEffect = new AllEffect(descriptor);
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
            var descriptor = new CombinatorEffectDescriptor(new[] { effectMock1, effectMock2 });
            var allEffect = new AllEffect(descriptor);

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
        public void AllEffect_Error()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );

            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var sagaCoroutineMock2 = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            sagaCoroutineMock.StartCoroutine(Arg.Any<IEnumerator>()).Returns(sagaCoroutineMock2);
            sagaCoroutineMock2.IsCompleted.Returns(true);

            var descriptor = new CombinatorEffectDescriptor(null);
            var allEffect = new AllEffect(descriptor);
            // execute
            effectRunner.RunEffect(allEffect, sagaCoroutineMock).TestRun();
            // verify
            sagaCoroutineMock.Received().SetError(Arg.Is<Exception>(
                e => e.GetType() == typeof(ArgumentNullException) &&
                     e.Message == "Value cannot be null.\nParameter name: source"
            ));
            sagaCoroutineMock2.Received().RequestCancel();
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
            var descriptor = new CombinatorEffectDescriptor(new[] { effectMock1, effectMock2 });
            var allEffect = new RaceEffect(descriptor);
            // execute
            effectRunner.RunEffect(allEffect, sagaCoroutineMock).TestRun();
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
            var descriptor = new CombinatorEffectDescriptor(new[] { effectMock1, effectMock2 });
            var allEffect = new RaceEffect(descriptor);

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
        public void RaceEffect_Error()
        {
            // setup
            var effectRunner = new EffectRunner<MockState>(
                _getStateMock,
                _dispatchMock,
                _subjectMock
            );

            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var sagaCoroutineMock2 = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            sagaCoroutineMock.StartCoroutine(Arg.Any<IEnumerator>()).Returns(sagaCoroutineMock2);
            sagaCoroutineMock2.IsCompleted.Returns(true);

            var descriptor = new CombinatorEffectDescriptor(null);
            var allEffect = new RaceEffect(descriptor);
            // execute
            effectRunner.RunEffect(allEffect, sagaCoroutineMock).TestRun();
            // verify
            sagaCoroutineMock.Received().SetError(Arg.Is<Exception>(
                e => e.GetType() == typeof(ArgumentNullException) &&
                     e.Message == "Value cannot be null.\nParameter name: source"
            ));
            sagaCoroutineMock2.Received().RequestCancel();
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
            var fnMock = Substitute.For<Func<object[], IEnumerator>>();
            fnMock.Invoke(Arg.Any<object[]>()).Returns(Enumerator.Empty);
            var args = new[] { (object)1, "test" };
            var callEffectDescriptor = new CallEffect.Descriptor(fnMock, args);
            var callEffect = new CallEffect(callEffectDescriptor);
            // execute
            effectRunner.RunEffect(callEffect, sagaCoroutineMock).TestRun();
            // verify
            fnMock
                .Received()
                .Invoke(
                    Arg.Do<object[]>(
                        objects =>
                        {
                            Assert.AreEqual(sagaCoroutineMock, objects[0]);
                            Assert.AreEqual(args[0], objects[1]);
                            Assert.AreEqual(args[1], objects[2]);
                        }
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
            var callEffectDescriptor = new CancelEffect.Descriptor(sagaCoroutineMock2);
            var callEffect = new CancelEffect(callEffectDescriptor);
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
            var callEffectDescriptor = new CancelEffect.Descriptor(null);
            var callEffect = new CancelEffect(callEffectDescriptor);
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
            var selectorMock = Substitute.For<Func<object, object[], object>>();
            selectorMock(state, args).Returns(returnValue);
            var setResultValueMock = Substitute.For<Action<object>>();
            var callEffectDescriptor =
                new SelectEffect.Descriptor(selectorMock, args, setResultValueMock);
            var callEffect = new SelectEffect(callEffectDescriptor);
            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            // execute
            effectRunner.RunEffect(callEffect, sagaCoroutineMock).TestRun();
            // verify
            setResultValueMock.Received()(returnValue);
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
            var effectDescriptor = new PutEffect.Descriptor(action);
            var putEffect = new PutEffect(effectDescriptor);
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
            var effectDescriptor = new TakeEffect.Descriptor(a => a == action);
            var effect = new TakeEffect(effectDescriptor);
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
            var effectDescriptor = new TakeEffect.Descriptor(a => false);
            var effect = new TakeEffect(effectDescriptor);
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
            var setResultValueMock = Substitute.For<Action<object>>();
            var internalSagaMock = Substitute.For<InternalSaga>();
            var effectDescriptor = new ForkEffect.Descriptor(internalSagaMock, args, setResultValueMock);
            var effect = new ForkEffect(effectDescriptor);
            var sagaCoroutineMock = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            var sagaCoroutineMock2 = Substitute.For<SagaCoroutine>(effectRunner, Enumerator.Empty);
            sagaCoroutineMock.StartCoroutine(Arg.Any<IEnumerator>()).Returns(sagaCoroutineMock2);
            // execute
            effectRunner.RunEffect(effect, sagaCoroutineMock);
            // verify
            internalSagaMock.Received()(args);
            setResultValueMock.Received()(sagaCoroutineMock2);
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
            var effectDescriptor = new JoinEffect.Descriptor(waitSagaCoroutineMock);
            var effect = new JoinEffect(effectDescriptor);
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
            var effectDescriptor = new JoinEffect.Descriptor(waitSagaCoroutineMock);
            var effect = new JoinEffect(effectDescriptor);
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
            var effectDescriptor = new JoinEffect.Descriptor(waitSagaCoroutineMock);
            var effect = new JoinEffect(effectDescriptor);
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
            var effectDescriptor = new JoinEffect.Descriptor(waitSagaCoroutineMock);
            var effect = new JoinEffect(effectDescriptor);
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
// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections;
using NSubstitute;
using NUnit.Framework;
using UniSaga.Core;

namespace UniSaga.Test.Core
{
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
            sagaCoroutineMock.StartCoroutine(Arg.Any<IEnumerator>()).Returns(sagaCoroutineMock2);
            sagaCoroutineMock2.IsCompleted.Returns(true);

            //var sagaCoroutine = SagaCoroutine.StartCoroutine(effectRunner, Enumerator.Empty);
            var effectMock = Substitute.For<IEffect>();
            var descriptor = new CombinatorEffectDescriptor(new[] { effectMock });
            var allEffect = new AllEffect(descriptor);
            // execute
            effectRunner.RunEffect(allEffect, sagaCoroutineMock).TestRun();
            // verify
            sagaCoroutineMock2.Received().RequestCancel();
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
            var setResultValueMock = Substitute.For<Action<object>>();
            var args = new[] { (object)1, "test" };
            var callEffectDescriptor = new CallEffect.Descriptor(fnMock, args, setResultValueMock);
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

        // ReSharper disable once ClassNeverInstantiated.Local
        private sealed class MockState
        {
        }
    }
}
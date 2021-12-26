// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using NUnit.Framework;
using UniSaga.Core;

namespace UniSaga.Test.Core
{
    [ExcludeFromCodeCoverage]
    internal class EffectHelperTest
    {
        // ReSharper disable once NUnit.TestCaseSourceShouldImplementIEnumerable
        [TestCaseSource(nameof(TakeLatestHelperArguments))]
        public void TakeLatestHelper(TakeLatestHelperTestCase testCase)
        {
            // setup
            var e = EffectHelper.TakeLatestHelper(testCase.Args);
            object[] forkArgs = null;
            InternalSaga forkInternalSaga = null;
            Func<object, bool> takePattern = null;
            SagaCoroutine cancelCoroutine = null;
            var forkCount = 0;
            var takeCount = 0;
            var cancelCount = 0;
            var count = 0;
            // execute
            while (e.MoveNext())
            {
                count++;
                var effect = e.Current;
                switch (effect)
                {
                    case ForkEffect forkEffect:
                        forkCount++;
                        forkArgs = forkEffect.EffectDescriptor.Args;
                        forkInternalSaga = forkEffect.EffectDescriptor.InternalSaga;
                        forkEffect.EffectDescriptor.SetResultValue!.Invoke(testCase.Coroutine);
                        break;
                    case TakeEffect takeEffect:
                        takeCount++;
                        takePattern = takeEffect.EffectDescriptor.Pattern;
                        break;
                    case CancelEffect cancelEffect:
                        cancelCount++;
                        cancelCoroutine = cancelEffect.EffectDescriptor.Coroutine;
                        break;
                    default:
                        throw new Exception();
                }

                if (count >= testCase.MaxCount) break;
            }

            // verify
            CollectionAssert.AreEqual(testCase.ExpectedArgs, forkArgs);
            Assert.AreEqual(testCase.ExpectedInternalSaga, forkInternalSaga);
            Assert.AreEqual(testCase.ExpectedPattern, takePattern);
            Assert.AreEqual(testCase.ExpectedCoroutine, cancelCoroutine);
            Assert.AreEqual(testCase.ExpectedForkCount, forkCount);
            Assert.AreEqual(testCase.ExpectedTakeCount, takeCount);
            Assert.AreEqual(testCase.ExpectedCancelCount, cancelCount);
            Assert.AreEqual(testCase.MaxCount, count);
        }

        public static IEnumerable<TakeLatestHelperTestCase> TakeLatestHelperArguments()
        {
            var pattern = Substitute.For<Func<object, bool>>();
            var internalSaga = Substitute.For<InternalSaga>();
            {
                var sagaCoroutine = Substitute.For<SagaCoroutine>(Substitute.For<IEffectRunner>(), Enumerator.Empty);
                sagaCoroutine.IsCompleted.Returns(false);
                yield return new TakeLatestHelperTestCase(
                    "引数なし且つsagaCoroutineがIsCompletedがfalseの時、Fork, Take, Cancel が正しく呼ばれること",
                    new[]
                    {
                        (object)pattern,
                        internalSaga
                    },
                    sagaCoroutine,
                    5,
                    Array.Empty<object>(),
                    internalSaga,
                    pattern,
                    sagaCoroutine,
                    2,
                    2,
                    1
                );
            }
            {
                var sagaCoroutine = Substitute.For<SagaCoroutine>(Substitute.For<IEffectRunner>(), Enumerator.Empty);
                sagaCoroutine.IsCompleted.Returns(true);
                yield return new TakeLatestHelperTestCase(
                    "引数あり且つsagaCoroutineがIsCompletedがtrueの時、Fork, Take が正しく呼ばれ、Cancelが呼ばれないこと",
                    new[]
                    {
                        (object)pattern,
                        internalSaga,
                        1,
                        "test2"
                    },
                    sagaCoroutine,
                    5,
                    new[]
                    {
                        (object)1,
                        "test2"
                    },
                    internalSaga,
                    pattern,
                    null,
                    2,
                    3,
                    0
                );
            }
        }

        // ReSharper disable once NUnit.TestCaseSourceShouldImplementIEnumerable
        [TestCaseSource(nameof(TakeEveryHelperArguments))]
        public void TakeEveryHelper(TakeEveryHelperTestCase testCase)
        {
            // setup
            var e = EffectHelper.TakeEveryHelper(testCase.Args);
            object[] forkArgs = null;
            InternalSaga forkInternalSaga = null;
            Func<object, bool> takePattern = null;
            Action<SagaCoroutine> takeSetResultValue = coroutine => { };
            var forkCount = 0;
            var takeCount = 0;
            var count = 0;
            // execute
            while (e.MoveNext())
            {
                count++;
                var effect = e.Current;
                switch (effect)
                {
                    case ForkEffect forkEffect:
                        forkCount++;
                        forkArgs = forkEffect.EffectDescriptor.Args;
                        forkInternalSaga = forkEffect.EffectDescriptor.InternalSaga;
                        takeSetResultValue = forkEffect.EffectDescriptor.SetResultValue;
                        break;
                    case TakeEffect takeEffect:
                        takeCount++;
                        takePattern = takeEffect.EffectDescriptor.Pattern;
                        break;
                    default:
                        throw new Exception();
                }

                if (count >= testCase.MaxCount) break;
            }

            // verify
            CollectionAssert.AreEqual(testCase.ExpectedArgs, forkArgs);
            Assert.AreEqual(testCase.ExpectedInternalSaga, forkInternalSaga);
            Assert.AreEqual(testCase.ExpectedPattern, takePattern);
            Assert.Null(takeSetResultValue);
            Assert.AreEqual(testCase.ExpectedForkCount, forkCount);
            Assert.AreEqual(testCase.ExpectedTakeCount, takeCount);
            Assert.AreEqual(testCase.MaxCount, count);
        }

        public static IEnumerable<TakeEveryHelperTestCase> TakeEveryHelperArguments()
        {
            var pattern = Substitute.For<Func<object, bool>>();
            var internalSaga = Substitute.For<InternalSaga>();
            {
                var sagaCoroutine = Substitute.For<SagaCoroutine>(Substitute.For<IEffectRunner>(), Enumerator.Empty);
                sagaCoroutine.IsCompleted.Returns(false);
                yield return new TakeEveryHelperTestCase(
                    "引数なしの時、Fork, Takeが正しく呼ばれること",
                    new[]
                    {
                        (object)pattern,
                        internalSaga
                    },
                    5,
                    Array.Empty<object>(),
                    internalSaga,
                    pattern,
                    2,
                    3
                );
            }
            {
                var sagaCoroutine = Substitute.For<SagaCoroutine>(Substitute.For<IEffectRunner>(), Enumerator.Empty);
                sagaCoroutine.IsCompleted.Returns(true);
                yield return new TakeEveryHelperTestCase(
                    "引数ありの時、Fork, Take が正しく呼ばれること",
                    new[]
                    {
                        (object)pattern,
                        internalSaga,
                        1,
                        "test2"
                    },
                    5,
                    new[]
                    {
                        (object)1,
                        "test2"
                    },
                    internalSaga,
                    pattern,
                    2,
                    3
                );
            }
        }


        // ReSharper disable once NUnit.TestCaseSourceShouldImplementIEnumerable
        [TestCaseSource(nameof(ExceptionTestCaseArguments))]
        public void TakeLatestHelper_Exception(ExceptionTestCase testCase)
        {
            // execute
            void TestDelegate()
            {
                EffectHelper.TakeLatestHelper(testCase.Args);
            }

            // verify
            var exception = Assert.Throws(testCase.ExpectedExceptionType, TestDelegate);
            Assert.AreEqual(testCase.ExpectedExceptionMessage, exception.Message);
        }

        // ReSharper disable once NUnit.TestCaseSourceShouldImplementIEnumerable
        [TestCaseSource(nameof(ExceptionTestCaseArguments))]
        public void TakeEveryHelper_Exception(ExceptionTestCase testCase)
        {
            // execute
            void TestDelegate()
            {
                EffectHelper.TakeEveryHelper(testCase.Args);
            }

            // verify
            var exception = Assert.Throws(testCase.ExpectedExceptionType, TestDelegate);
            Assert.AreEqual(testCase.ExpectedExceptionMessage, exception.Message);
        }

        public static IEnumerable<ExceptionTestCase> ExceptionTestCaseArguments()
        {
            var pattern = Substitute.For<Func<object, bool>>();
            var internalSaga = Substitute.For<InternalSaga>();

            yield return new ExceptionTestCase(
                "引数が足りない時、期待する例外が投げられること",
                Array.Empty<object>(),
                typeof(InvalidOperationException),
                "Not enough arguments."
            );

            yield return new ExceptionTestCase(
                "第１引数の型が異なる時、期待する例外が投げられること",
                new[]
                {
                    (object)internalSaga,
                    internalSaga
                },
                typeof(InvalidOperationException),
                "The first argument must be one of type Func. UniSaga.Core.InternalSaga"
            );

            yield return new ExceptionTestCase(
                "第２引数の型が異なる時、期待する例外が投げられること",
                new[]
                {
                    (object)pattern,
                    pattern
                },
                typeof(InvalidOperationException),
                "The second argument must be one of type InternalSaga. System.Func`2[[System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Boolean, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"
            );
        }
    }

    internal class TakeLatestHelperTestCase
    {
        public TakeLatestHelperTestCase(
            string displayName,
            object[] args,
            SagaCoroutine coroutine,
            int maxCount,
            object[] expectedArgs,
            InternalSaga expectedInternalSaga,
            Func<object, bool> expectedPattern,
            SagaCoroutine expectedCoroutine,
            int expectedForkCount,
            int expectedTakeCount,
            int expectedCancelCount)
        {
            DisplayName = displayName;
            Args = args;
            Coroutine = coroutine;
            MaxCount = maxCount;
            ExpectedArgs = expectedArgs;
            ExpectedInternalSaga = expectedInternalSaga;
            ExpectedPattern = expectedPattern;
            ExpectedCoroutine = expectedCoroutine;
            ExpectedForkCount = expectedForkCount;
            ExpectedTakeCount = expectedTakeCount;
            ExpectedCancelCount = expectedCancelCount;
        }

        private string DisplayName { get; }
        public object[] Args { get; }
        public SagaCoroutine Coroutine { get; }
        public int MaxCount { get; }
        public object[] ExpectedArgs { get; }
        public InternalSaga ExpectedInternalSaga { get; }
        public Func<object, bool> ExpectedPattern { get; }
        public SagaCoroutine ExpectedCoroutine { get; }
        public int ExpectedForkCount { get; }
        public int ExpectedTakeCount { get; }
        public int ExpectedCancelCount { get; }

        public override string ToString()
        {
            return DisplayName ?? base.ToString();
        }
    }

    internal class ExceptionTestCase
    {
        public ExceptionTestCase(
            string displayName,
            object[] args,
            Type expectedExceptionType,
            string expectedExceptionMessage)
        {
            DisplayName = displayName;
            Args = args;
            ExpectedExceptionType = expectedExceptionType;
            ExpectedExceptionMessage = expectedExceptionMessage;
        }

        private string DisplayName { get; }
        public object[] Args { get; }
        public Type ExpectedExceptionType { get; }
        public string ExpectedExceptionMessage { get; }

        public override string ToString()
        {
            return DisplayName ?? base.ToString();
        }
    }

    internal class TakeEveryHelperTestCase
    {
        public TakeEveryHelperTestCase(
            string displayName,
            object[] args,
            int maxCount,
            object[] expectedArgs,
            InternalSaga expectedInternalSaga,
            Func<object, bool> expectedPattern,
            int expectedForkCount,
            int expectedTakeCount)
        {
            DisplayName = displayName;
            Args = args;
            MaxCount = maxCount;
            ExpectedArgs = expectedArgs;
            ExpectedInternalSaga = expectedInternalSaga;
            ExpectedPattern = expectedPattern;
            ExpectedForkCount = expectedForkCount;
            ExpectedTakeCount = expectedTakeCount;
        }

        private string DisplayName { get; }
        public object[] Args { get; }
        public int MaxCount { get; }
        public object[] ExpectedArgs { get; }
        public InternalSaga ExpectedInternalSaga { get; }
        public Func<object, bool> ExpectedPattern { get; }
        public int ExpectedForkCount { get; }
        public int ExpectedTakeCount { get; }

        public override string ToString()
        {
            return DisplayName ?? base.ToString();
        }
    }
}
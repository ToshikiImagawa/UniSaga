// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections;
using JetBrains.Annotations;

namespace UniSaga.Core
{
    internal delegate IEnumerator InternalSaga(params object[] arguments);

    internal abstract class CombinatorEffect : IEffect
    {
        public bool Combinator => true;
        public abstract object Payload { get; }
    }

    internal abstract class SimpleEffect : IEffect
    {
        public bool Combinator => false;
        public abstract object Payload { get; }
    }

    internal interface ICombinatorEffectDescriptor
    {
        IEffect[] Effects { get; }
    }

    internal class RaceEffect : CombinatorEffect
    {
        public RaceEffect(RaceEffectDescriptor payload)
        {
            Payload = payload;
        }

        public override object Payload { get; }

        public class RaceEffectDescriptor : ICombinatorEffectDescriptor
        {
            public RaceEffectDescriptor(IEffect[] effects)
            {
                Effects = effects;
            }

            public IEffect[] Effects { get; }
        }
    }

    internal class AllEffect : CombinatorEffect
    {
        public AllEffect(Descriptor payload)
        {
            Payload = payload;
        }

        public override object Payload { get; }

        public class Descriptor : ICombinatorEffectDescriptor
        {
            public Descriptor(IEffect[] effects)
            {
                Effects = effects;
            }

            public IEffect[] Effects { get; }
        }
    }

    internal class JoinEffect : SimpleEffect
    {
        public JoinEffect(Descriptor payload)
        {
            Payload = payload;
        }

        public override object Payload { get; }

        public class Descriptor : CallEffect.Descriptor
        {
            public Descriptor(SagaTask sagaTask) : base(
                sagaTask,
                a => Empty(),
                Array.Empty<object>(),
                null
            )
            {
            }

            private static IEnumerator Empty()
            {
                yield break;
            }
        }
    }

    internal class ForkEffect : SimpleEffect
    {
        public ForkEffect([NotNull] Descriptor payload)
        {
            Payload = payload;
        }

        public override object Payload { get; }

        public class Descriptor : CallEffect.Descriptor
        {
            public Descriptor(
                [NotNull] object context,
                [NotNull] object[] args,
                [CanBeNull] Action<object> setResultValue
            ) : base(
                context,
                a => Empty(),
                args,
                setResultValue
            )
            {
            }

            private static IEnumerator Empty()
            {
                yield break;
            }
        }
    }

    internal class TakeEffect : SimpleEffect
    {
        public TakeEffect([NotNull] Descriptor payload)
        {
            Payload = payload;
        }

        public override object Payload { get; }

        public class Descriptor
        {
            public Descriptor(Func<object, bool> pattern)
            {
                Pattern = pattern;
            }

            public Func<object, bool> Pattern { get; }
        }
    }

    internal class PutEffect : SimpleEffect
    {
        public PutEffect([NotNull] Descriptor innerPayload)
        {
            Payload = innerPayload;
        }

        public override object Payload { get; }

        public class Descriptor
        {
            public Descriptor(object action)
            {
                Action = action;
            }

            public object Action { get; }
        }
    }

    internal class SelectEffect : SimpleEffect
    {
        public SelectEffect([NotNull] Descriptor descriptor)
        {
            Payload = descriptor;
        }

        public override object Payload { get; }

        public class Descriptor
        {
            public Descriptor(
                [NotNull] Func<object, object[], object> selector,
                [NotNull] object[] args,
                [NotNull] Action<object> setResultValue)
            {
                Selector = selector;
                Args = args;
                SetResultValue = setResultValue;
            }

            [NotNull] public Func<object, object[], object> Selector { get; }
            [NotNull] public object[] Args { get; }
            [NotNull] public Action<object> SetResultValue { get; }
        }
    }

    internal sealed class CancelEffect : SimpleEffect
    {
        public CancelEffect(Descriptor payload)
        {
            Payload = payload;
        }

        public override object Payload { get; }

        public class Descriptor

        {
            public Descriptor([CanBeNull] SagaTask task)
            {
                Task = task;
            }

            [CanBeNull] public SagaTask Task { get; }
        }
    }

    internal class CallEffect : SimpleEffect
    {
        public CallEffect([NotNull] Descriptor descriptor)
        {
            Payload = descriptor;
        }

        public override object Payload { get; }

        public class Descriptor
        {
            public Descriptor(
                [NotNull] Func<object[], IEnumerator> fn,
                [NotNull] object[] args,
                [CanBeNull] Action<object> setResultValue)
            {
                Context = null;
                Fn = fn;
                Args = args;
                SetResultValue = setResultValue;
            }

            protected Descriptor(
                [CanBeNull] object context,
                [NotNull] Func<object[], IEnumerator> fn,
                [NotNull] object[] args,
                [CanBeNull] Action<object> setResultValue
            )
            {
                Context = context;
                Fn = fn;
                Args = args;
                SetResultValue = setResultValue;
            }

            [CanBeNull] public object Context { get; }
            [NotNull] public Func<object[], IEnumerator> Fn { get; }
            [NotNull] public object[] Args { get; }
            [CanBeNull] public Action<object> SetResultValue { get; }
        }
    }
}
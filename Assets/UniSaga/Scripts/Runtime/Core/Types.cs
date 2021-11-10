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
    }

    internal abstract class SimpleEffect : IEffect
    {
        public bool Combinator => false;
    }

    internal sealed class CombinatorEffectDescriptor
    {
        public CombinatorEffectDescriptor(IEffect[] effects)
        {
            Effects = effects;
        }

        public IEffect[] Effects { get; }
    }

    internal sealed class RaceEffect : CombinatorEffect
    {
        public RaceEffect(CombinatorEffectDescriptor payload)
        {
            EffectDescriptor = payload;
        }

        public CombinatorEffectDescriptor EffectDescriptor { get; }
    }

    internal sealed class AllEffect : CombinatorEffect
    {
        public AllEffect(CombinatorEffectDescriptor payload)
        {
            EffectDescriptor = payload;
        }

        public CombinatorEffectDescriptor EffectDescriptor { get; }
    }

    internal sealed class JoinEffect : SimpleEffect
    {
        public JoinEffect(Descriptor payload)
        {
            EffectDescriptor = payload;
        }

        public Descriptor EffectDescriptor { get; }

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

    internal sealed class ForkEffect : SimpleEffect
    {
        public ForkEffect([NotNull] Descriptor payload)
        {
            EffectDescriptor = payload;
        }

        public Descriptor EffectDescriptor { get; }

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

    internal sealed class TakeEffect : SimpleEffect
    {
        public TakeEffect([NotNull] Descriptor payload)
        {
            EffectDescriptor = payload;
        }

        public Descriptor EffectDescriptor { get; }

        public class Descriptor
        {
            public Descriptor(Func<object, bool> pattern)
            {
                Pattern = pattern;
            }

            public Func<object, bool> Pattern { get; }
        }
    }

    internal sealed class PutEffect : SimpleEffect
    {
        public PutEffect([NotNull] Descriptor innerPayload)
        {
            EffectDescriptor = innerPayload;
        }

        public Descriptor EffectDescriptor { get; }

        public class Descriptor
        {
            public Descriptor(object action)
            {
                Action = action;
            }

            public object Action { get; }
        }
    }

    internal sealed class SelectEffect : SimpleEffect
    {
        public SelectEffect([NotNull] Descriptor descriptor)
        {
            EffectDescriptor = descriptor;
        }

        public Descriptor EffectDescriptor { get; }

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
            EffectDescriptor = payload;
        }

        public Descriptor EffectDescriptor { get; }

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
            EffectDescriptor = descriptor;
        }

        public Descriptor EffectDescriptor { get; }

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
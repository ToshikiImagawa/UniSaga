// Copyright @2021 COMCREATE. All rights reserved.

using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace UniSaga.Core
{
    internal abstract class SimpleEffect<TPayload> : IEffect where TPayload : class
    {
        public bool Combinator => false;
        public abstract TPayload Payload { get; }
        object IEffect.Payload => Payload;
    }

    internal interface ISelectEffectDescriptor
    {
        [NotNull] Func<object, object[], object> Selector { get; }
        [NotNull] object[] Args { get; }
        [NotNull] Action<object> SetResultValue { get; }
    }

    internal interface ICallEffectDescriptor
    {
        [CanBeNull] object Context { get; }
        [NotNull] Func<object[], UniTask<object>> Fn { get; }
        [NotNull] object[] Args { get; }
        [CanBeNull] Action<object> SetResultValue { get; }
    }

    internal interface IPutEffectDescriptor
    {
        object Action { get; }
    }

    internal interface ITakeEffectDescriptor
    {
        Func<object, bool> Pattern { get; }
    }

    internal interface IForkEffectDescriptor : ICallEffectDescriptor
    {
        bool Detached { get; }
    }

    internal class ForkEffect : SimpleEffect<IForkEffectDescriptor>
    {
        public ForkEffect(IForkEffectDescriptor payload)
        {
            Payload = payload;
        }

        public override IForkEffectDescriptor Payload { get; }

        public class ForkEffectDescriptor : IForkEffectDescriptor
        {
            public ForkEffectDescriptor(
                [NotNull] Func<object[], UniTask<object>> fn,
                [NotNull] object[] args,
                [CanBeNull] Action<object> setResultValue,
                bool detached
            )
            {
                Context = null;
                Fn = fn;
                Args = args;
                SetResultValue = setResultValue;
                Detached = detached;
            }

            public object Context { get; }
            public Func<object[], UniTask<object>> Fn { get; }
            public object[] Args { get; }
            public Action<object> SetResultValue { get; }
            public bool Detached { get; }
        }
    }

    internal class TakeEffect : SimpleEffect<ITakeEffectDescriptor>
    {
        public TakeEffect(ITakeEffectDescriptor payload)
        {
            Payload = payload;
        }

        public override ITakeEffectDescriptor Payload { get; }

        public class TakeEffectDescriptor : ITakeEffectDescriptor
        {
            public TakeEffectDescriptor(Func<object, bool> pattern)
            {
                Pattern = pattern;
            }

            public Func<object, bool> Pattern { get; }
        }
    }

    internal class PutEffect : SimpleEffect<IPutEffectDescriptor>
    {
        public PutEffect([NotNull] IPutEffectDescriptor innerPayload)
        {
            Payload = innerPayload;
        }

        public override IPutEffectDescriptor Payload { get; }

        public class PutEffectDescriptor : IPutEffectDescriptor
        {
            public PutEffectDescriptor(object action)
            {
                Action = action;
            }

            public object Action { get; }
        }
    }

    internal class SelectEffect : SimpleEffect<ISelectEffectDescriptor>
    {
        public SelectEffect([NotNull] ISelectEffectDescriptor descriptor)
        {
            Payload = descriptor;
        }

        public override ISelectEffectDescriptor Payload { get; }

        public class SelectEffectDescriptor : ISelectEffectDescriptor
        {
            public SelectEffectDescriptor(
                [NotNull] Func<object, object[], object> selector,
                [NotNull] object[] args,
                [NotNull] Action<object> setResultValue)
            {
                Selector = selector;
                Args = args;
                SetResultValue = setResultValue;
            }

            public Func<object, object[], object> Selector { get; }
            public object[] Args { get; }
            public Action<object> SetResultValue { get; }
        }
    }

    internal class CallEffect : SimpleEffect<ICallEffectDescriptor>
    {
        public CallEffect([NotNull] ICallEffectDescriptor descriptor)
        {
            Payload = descriptor;
        }

        public override ICallEffectDescriptor Payload { get; }

        public class CallEffectDescriptor : ICallEffectDescriptor
        {
            public CallEffectDescriptor(
                [NotNull] Func<object[], UniTask<object>> fn,
                [NotNull] object[] args,
                [CanBeNull] Action<object> setResultValue
            )
            {
                Context = null;
                Fn = fn;
                Args = args;
                SetResultValue = setResultValue;
            }

            public CallEffectDescriptor(
                [CanBeNull] object context,
                [NotNull] Func<object[], UniTask<object>> fn,
                [NotNull] object[] args,
                [CanBeNull] Action<object> setResultValue
            )
            {
                Context = context;
                Fn = fn;
                Args = args;
                SetResultValue = setResultValue;
            }

            public object Context { get; }
            public Func<object[], UniTask<object>> Fn { get; }
            public object[] Args { get; }
            public Action<object> SetResultValue { get; }
        }
    }
}
// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace UniSaga.Core
{
    internal delegate IEnumerator<IEffect> InternalSaga(params object[] arguments);

    internal abstract class CombinatorEffect<TPayload> : IEffect where TPayload : class
    {
        public bool Combinator => true;
        [NotNull] public abstract TPayload Payload { get; }
        object IEffect.Payload => Payload;
    }

    internal abstract class SimpleEffect<TPayload> : IEffect where TPayload : class
    {
        public bool Combinator => false;
        [NotNull] public abstract TPayload Payload { get; }
        object IEffect.Payload => Payload;
    }

    internal interface ISelectEffectDescriptor
    {
        [NotNull] Func<object, object[], object> Selector { get; }
        [NotNull] object[] Args { get; }
        [NotNull] Action<object> SetResultValue { get; }
    }

    internal interface ICancelEffectDescriptor
    {
        [CanBeNull] SagaTask Task { get; }
    }

    internal interface ICallEffectDescriptor
    {
        [CanBeNull] object Context { get; }
        [NotNull] Func<object[], CancellationToken, UniTask<object>> Fn { get; }
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
    }

    internal interface IJoinEffectDescriptor : ICallEffectDescriptor
    {
    }

    internal interface ICombinatorEffectDescriptor
    {
        IEffect[] Effects { get; }
    }

    internal interface IAllEffectDescriptor : ICombinatorEffectDescriptor
    {
    }

    internal interface IRaceEffectDescriptor : ICombinatorEffectDescriptor
    {
    }

    internal class RaceEffect : CombinatorEffect<IRaceEffectDescriptor>
    {
        public RaceEffect(IRaceEffectDescriptor payload)
        {
            Payload = payload;
        }

        public override IRaceEffectDescriptor Payload { get; }

        public class RaceEffectDescriptor : IRaceEffectDescriptor
        {
            public RaceEffectDescriptor(IEffect[] effects)
            {
                Effects = effects;
            }

            public IEffect[] Effects { get; }
        }
    }

    internal class AllEffect : CombinatorEffect<IAllEffectDescriptor>
    {
        public AllEffect(IAllEffectDescriptor payload)
        {
            Payload = payload;
        }

        public override IAllEffectDescriptor Payload { get; }

        public class AllEffectDescriptor : IAllEffectDescriptor
        {
            public AllEffectDescriptor(IEffect[] effects)
            {
                Effects = effects;
            }

            public IEffect[] Effects { get; }
        }
    }

    internal class JoinEffect : SimpleEffect<IJoinEffectDescriptor>
    {
        public JoinEffect(IJoinEffectDescriptor payload)
        {
            Payload = payload;
        }

        public override IJoinEffectDescriptor Payload { get; }

        public class JoinEffectDescriptor : IJoinEffectDescriptor
        {
            public JoinEffectDescriptor(SagaTask sagaTask)
            {
                Context = sagaTask;
                Fn = (a, t) => UniTask.Never<object>(t);
                Args = Array.Empty<object>();
                SetResultValue = null;
            }

            public object Context { get; }
            public Func<object[], CancellationToken, UniTask<object>> Fn { get; }
            public object[] Args { get; }
            public Action<object> SetResultValue { get; }
        }
    }

    internal class ForkEffect : SimpleEffect<IForkEffectDescriptor>
    {
        public ForkEffect([NotNull] IForkEffectDescriptor payload)
        {
            Payload = payload;
        }

        public override IForkEffectDescriptor Payload { get; }

        public class ForkEffectDescriptor : IForkEffectDescriptor
        {
            public ForkEffectDescriptor(
                [NotNull] object context,
                [NotNull] object[] args,
                [CanBeNull] Action<object> setResultValue,
                bool detached
            )
            {
                Context = context;
                Fn = (a, t) => UniTask.Never<object>(t);
                Args = args;
                SetResultValue = setResultValue;
                Detached = detached;
            }

            public object Context { get; }
            public Func<object[], CancellationToken, UniTask<object>> Fn { get; }
            public object[] Args { get; }
            public Action<object> SetResultValue { get; }
            public bool Detached { get; }
        }
    }

    internal class TakeEffect : SimpleEffect<ITakeEffectDescriptor>
    {
        public TakeEffect([NotNull] ITakeEffectDescriptor payload)
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

    internal sealed class CancelEffect : SimpleEffect<ICancelEffectDescriptor>
    {
        public CancelEffect(ICancelEffectDescriptor payload)
        {
            Payload = payload;
        }

        public override ICancelEffectDescriptor Payload { get; }

        public class CancelEffectDescriptor : ICancelEffectDescriptor
        {
            public CancelEffectDescriptor([CanBeNull] SagaTask task)
            {
                Task = task;
            }

            public SagaTask Task { get; }
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
                [NotNull] Func<object[], CancellationToken, UniTask<object>> fn,
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
                [NotNull] Func<object[], CancellationToken, UniTask<object>> fn,
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
            public Func<object[], CancellationToken, UniTask<object>> Fn { get; }
            public object[] Args { get; }
            public Action<object> SetResultValue { get; }
        }
    }
}
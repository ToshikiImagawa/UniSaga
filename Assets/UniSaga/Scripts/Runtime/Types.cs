// Copyright @2021 COMCREATE. All rights reserved.

namespace UniSaga
{
    public interface IEffect
    {
    }

    public sealed class ReturnData<T>
    {
        public T Value { get; private set; }

        internal void SetValue(T value)
        {
            Value = value;
        }
    }
}
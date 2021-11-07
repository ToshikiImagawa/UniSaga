// Copyright @2021 COMCREATE. All rights reserved.

namespace UniSaga.Sample
{
    public readonly struct SetIdAction
    {
        public SetIdAction(int id)
        {
            Id = id;
        }

        public int Id { get; }
    }

    public readonly struct RestartAction
    {
    }

    public readonly struct ErrorAction
    {
        public ErrorAction(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    public readonly struct StartAction
    {
    }
}
namespace UniSaga.Sample
{
    public static class SampleReducer
    {
        public static SampleState Execute(SampleState previousState, object action)
        {
            switch (action)
            {
                case SetIdAction setAction:
                    return new SampleState(setAction.Id);
                case ErrorAction _:
                    return new SampleState(0);
                default:
                    return previousState;
            }
        }
    }
}
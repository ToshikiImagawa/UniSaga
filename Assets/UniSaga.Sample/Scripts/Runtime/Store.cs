// Copyright @2021 COMCREATE. All rights reserved.

namespace UniSaga.Sample
{
    public static class Store
    {
        public static UniRedux.IStore<SampleState> ConfigureStore()
        {
            var uniSagaMiddleware = new UniSagaMiddleware<SampleState>();
            var store = UniRedux.Redux.CreateStore(
                SampleReducer.Execute,
                new SampleState(0),
                uniSagaMiddleware.Middleware
            );
            var _ = uniSagaMiddleware.Run(Sagas.RootSaga);
            return store;
        }
    }
}
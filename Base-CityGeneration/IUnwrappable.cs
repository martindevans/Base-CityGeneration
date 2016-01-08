using System;
using System.Diagnostics.Contracts;
using Myre.Collections;

namespace Base_CityGeneration
{
    [ContractClass(typeof(IUnwrappableContracts<>))]
    internal interface IUnwrappable<out T>
    {
        T Unwrap();
    }

    [ContractClassFor(typeof(IUnwrappable<>))]
    internal abstract class IUnwrappableContracts<T>
        : IUnwrappable<T>
    {
        public T Unwrap()
        {
            Contract.Ensures(Contract.Result<T>() != null);

            return default(T);
        }
    }

    [ContractClass(typeof(IUnwrappable2Contracts<>))]
    internal interface IUnwrappable2<out T>
    {
        T Unwrap(Func<double> random, INamedDataCollection metadata);
    }

    [ContractClassFor(typeof(IUnwrappable2<>))]
    internal abstract class IUnwrappable2Contracts<T>
        : IUnwrappable2<T>
    {
        public T Unwrap(Func<double> random, INamedDataCollection metadata)
        {
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Ensures(Contract.Result<T>() != null);

            return default(T);
        }
    }
}

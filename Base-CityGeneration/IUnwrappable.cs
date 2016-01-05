using System.Diagnostics.Contracts;

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
}

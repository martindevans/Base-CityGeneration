using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Base_CityGeneration
{
    /// <summary>
    /// Base interface for container classes which can be "unwrapped" into another type.
    /// Usually used for serialization, the container is the deserialized version, Unwrap() converts it into the model we really want
    /// </summary>
    /// <typeparam name="T">Type which this unwraps to</typeparam>
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

    public static class IUnwrappableExtensions
    {
        internal static IEnumerable<T> UnwrapEnumerable<T>(this IEnumerable<IUnwrappable<T>> collection)
        {
            Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);

            if (collection == null)
                return Array.Empty<T>();

            return collection.Select(a => a.Unwrap());
        }

        internal static T UnwrapNullable<T>(this IUnwrappable<T> unwrappable)
            where T : class
        {
            return unwrappable == null ? null : unwrappable.Unwrap();
        }
    }
}

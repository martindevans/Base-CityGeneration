using System;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    public struct Weighted<T>
    {
        public float Strength { get; private set; }
        public T Requirement { get; private set; }

        public Weighted(T requirement, float strength)
            : this()
        {
            Contract.Requires(requirement != null);
            Contract.Requires<ArgumentOutOfRangeException>(strength >= -1, "Strength must be >= -1");
            Contract.Requires<ArgumentOutOfRangeException>(strength <= 1, "Strength must be <= 1");

            Requirement = requirement;
            Strength = strength;
        }

        internal class Container<TContainerItem>
            : IUnwrappable<Weighted<T>>
        where TContainerItem : IUnwrappable<T>
        {
            public float Strength { get; [UsedImplicitly]set; }

            public TContainerItem Req { get; [UsedImplicitly] set; }

            public Weighted<T> Unwrap()
            {
                return new Weighted<T>(
                    Req.Unwrap(),
                    Strength
                );
            }
        }
    }
}

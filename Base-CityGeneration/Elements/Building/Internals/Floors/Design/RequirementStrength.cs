using System;
using System.Diagnostics.Contracts;
using Base_CityGeneration.Utilities.Extensions;
using Base_CityGeneration.Utilities.Numbers;
using JetBrains.Annotations;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    public struct RequirementStrength<T>
    {
        public float Strength { get; private set; }
        public T Requirement { get; private set; }

        public RequirementStrength(T requirement, float strength)
            : this()
        {
            Contract.Requires(requirement != null);
            Contract.Requires<ArgumentOutOfRangeException>(strength >= -1, "Strength must be >= -1");
            Contract.Requires<ArgumentOutOfRangeException>(strength <= 1, "Strength must be <= 1");

            Requirement = requirement;
            Strength = strength;
        }
    }

    internal class RequirementStrengthContainer<TItem, TContainerItem>
        : IUnwrappable2<RequirementStrength<TItem>>
        where TContainerItem : IUnwrappable2<TItem>
    {
        public object Strength { get; [UsedImplicitly]set; }

        public TContainerItem Req { get; [UsedImplicitly] set; }

        public RequirementStrength<TItem> Unwrap(Func<double> random, INamedDataCollection metadata)
        {
            return new RequirementStrength<TItem>(
                Req.Unwrap(random, metadata),
                IValueGeneratorContainer.FromObject(Strength).Transform(vary: false).SelectFloatValue(random, metadata)
            );
        }
    }
}

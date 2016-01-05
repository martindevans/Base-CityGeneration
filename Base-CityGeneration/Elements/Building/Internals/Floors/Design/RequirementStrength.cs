using System;
using System.Diagnostics.Contracts;
using Base_CityGeneration.Utilities.Extensions;
using Base_CityGeneration.Utilities.Numbers;
using JetBrains.Annotations;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    public struct RequirementStrength<T>
    {
        public IValueGenerator Strength { get; private set; }
        public T Requirement { get; private set; }

        public RequirementStrength(T requirement, IValueGenerator strength)
            : this()
        {
            Contract.Requires(strength != null);
            Contract.Requires<ArgumentOutOfRangeException>(strength.MinValue >= -1, "Minimum strength must be >= -1");
            Contract.Requires<ArgumentOutOfRangeException>(strength.MaxValue <= 1, "Maximum strength must be <= 1");

            Requirement = requirement;
            Strength = strength;
        }
    }

    internal class RequirementStrengthContainer<TItem, TContainerItem>
        : IUnwrappable<RequirementStrength<TItem>>
        where TContainerItem : IUnwrappable<TItem>
    {
        public object Strength { get; [UsedImplicitly]set; }

        public TContainerItem Req { get; [UsedImplicitly] set; }

        public RequirementStrength<TItem> Unwrap()
        {
            return new RequirementStrength<TItem>(
                Req.Unwrap(),
                IValueGeneratorContainer.FromObject(Strength).Transform(vary: false)
            );
        }
    }
}

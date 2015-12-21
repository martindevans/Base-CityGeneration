using System;
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
            Requirement = requirement;

            Strength = strength;
            if (Strength.MinValue < 0)
                throw new ArgumentOutOfRangeException("strength", "Minimum strength must be >= 0");
            if (Strength.MaxValue < 0)
                throw new ArgumentOutOfRangeException("strength", "Maximum strength must be <= 1");
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
                BaseValueGeneratorContainer.FromObject(Strength).Transform(vary: false)
            );
        }
    }
}

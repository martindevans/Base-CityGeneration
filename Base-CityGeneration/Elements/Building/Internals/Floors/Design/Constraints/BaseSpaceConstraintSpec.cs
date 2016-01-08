using System;
using System.Diagnostics.Contracts;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints
{
    [ContractClass(typeof(BaseSpaceConstraintSpecContracts))]
    public abstract class BaseSpaceConstraintSpec
    {
        internal abstract T Union<T>(T other) where T : BaseSpaceConstraintSpec;

        /// <summary>
        /// Return a value (0-1) which indicates how likely it is that this constraint will be satisfied in the given region
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public abstract float AssessSatisfactionProbability(FloorplanRegion region);

        /// <summary>
        /// Assess if the constraint is satisfied in the given region
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public abstract bool IsSatisfied(FloorplanRegion region);

        internal abstract class BaseContainer
            : IUnwrappable2<BaseSpaceConstraintSpec>
        {
            public abstract BaseSpaceConstraintSpec Unwrap(Func<double> random, INamedDataCollection metadata);
        }
    }

    [ContractClassFor(typeof(BaseSpaceConstraintSpec))]
    internal abstract class BaseSpaceConstraintSpecContracts
        : BaseSpaceConstraintSpec
    {
        internal override T Union<T>(T other)
        {
            Contract.Requires(other != null);
            Contract.Ensures(Contract.Result<T>() != null);

            return default(T);
        }

        public override float AssessSatisfactionProbability(FloorplanRegion region)
        {
            Contract.Requires(region != null);

            return default(float);
        }
    }
}

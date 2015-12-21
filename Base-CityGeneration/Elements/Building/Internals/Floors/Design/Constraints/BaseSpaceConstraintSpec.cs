using System;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints
{
    public abstract class BaseSpaceConstraintSpec
    {
        internal abstract T Union<T>(T other) where T : BaseSpaceConstraintSpec;

        /// <summary>
        /// Return a value (0-1) which indicates how likely it is that this constraint will be satisfied in the given region
        /// </summary>
        /// <param name="region"></param>
        /// <param name="random"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public abstract float AssessSatisfactionProbability(FloorplanRegion region, Func<double> random, INamedDataCollection metadata);

        internal abstract class BaseContainer
            : IUnwrappable<BaseSpaceConstraintSpec>
        {
            public abstract BaseSpaceConstraintSpec Unwrap();
        }
    }
}

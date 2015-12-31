﻿using System;
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

    [ContractClassFor(typeof(BaseSpaceConstraintSpec))]
    internal abstract class BaseSpaceConstraintSpecContracts
        : BaseSpaceConstraintSpec
    {
        internal override T Union<T>(T other)
        {
            Contract.Requires<ArgumentNullException>(other != null, "other");

            return default(T);
        }

        public override float AssessSatisfactionProbability(FloorplanRegion region, Func<double> random, INamedDataCollection metadata)
        {
            Contract.Requires<ArgumentNullException>(region != null, "region");
            Contract.Requires<ArgumentNullException>(metadata != null, "metadata");
            Contract.Requires<ArgumentNullException>(random != null, "random");

            return default(float);
        }
    }
}

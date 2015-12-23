using System;
using System.Linq;
using EpimetheusPlugins.Extensions;
using Myre.Collections;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints
{
    /// <summary>
    /// Indicates that this space must border an exterior wall
    /// </summary>
    public abstract class BaseExterior<TSelf>
        : BaseSpaceConstraintSpec
        where TSelf : BaseExterior<TSelf>
    {
        /// <summary>
        /// Indicates if this constraint *requires* the exterior feature or requires *not* that feature
        /// </summary>
        private readonly bool _require;

        private readonly Section.Types _sectionType;

        protected BaseExterior(bool require, Section.Types sectionType)
        {
            _require = require;
            _sectionType = sectionType;
        }

        public override float AssessSatisfactionProbability(FloorplanRegion region, Func<double> random, INamedDataCollection metadata)
        {
            //Count up how many exterior constraints already want things from this region
            var exteriorConstraints = from space in region.AssignedSpaces
                                      from sreq in space.Constraints
                                      let constraint = sreq.Requirement as TSelf
                                      where constraint != null
                                      select constraint;
            int positiveRequirements = 0;
            int negativeRequirements = 0;
            foreach (var assignedConstraints in exteriorConstraints)
            {
                positiveRequirements += assignedConstraints._require ? 1 : 0;
                negativeRequirements += !assignedConstraints._require ? 1 : 0;
            }

            //measure amount of available thing (whatever it is)
            var amount = TotalResourceLength(region, _sectionType);

            //measure amount of non-thing perimeter
            var none = region.Points.Perimeter() - amount;

            //Calculate ratio of thing:number who want it
            return MathHelper.Clamp(_require ? (amount / (positiveRequirements + 1)) : (none / (negativeRequirements + 1)), 0, 1);
        }

        private static float TotalResourceLength(FloorplanRegion region, Section.Types type)
        {
            return region
                .Shape
                .SelectMany(a => a.Sections)
                .Where(r => r.Type == type)
                .Select(r => (r.End - r.Start))
                .Sum();
        }
    }
}

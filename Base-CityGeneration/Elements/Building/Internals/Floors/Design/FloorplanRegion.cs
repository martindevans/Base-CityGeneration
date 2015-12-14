using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Datastructures;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Utilities;
using Myre.Collections;
using Vector2 = System.Numerics.Vector2;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    internal class FloorplanRegion
        : BasePolygonRegion<FloorplanRegion>
    {
        private readonly List<BaseSpaceSpec> _assignedSpaces = new List<BaseSpaceSpec>(); 
        public IReadOnlyList<BaseSpaceSpec> AssignedSpaces { get { return _assignedSpaces; } }
        public float AssignedSpaceArea { get; private set; }

        public FloorplanRegion(IReadOnlyList<Vector2> shape)
            : this(shape, OABR.Fit(shape))
        {
        }

        public FloorplanRegion(IReadOnlyList<Vector2> shape, OABR oabr)
            : base(shape, oabr)
        {
        }

        public void Add(BaseSpaceSpec spec, Func<double> random, INamedDataCollection metadata)
        {
            //Save this space
            _assignedSpaces.Add(spec);

            //Update the area consumed in this space (assuming the minimum)
            AssignedSpaceArea += spec.MinArea(random, metadata);
        }

        protected override FloorplanRegion Construct(IReadOnlyList<Vector2> shape, OABR oabr)
        {
            return new FloorplanRegion(shape, oabr);
        }
    }
}

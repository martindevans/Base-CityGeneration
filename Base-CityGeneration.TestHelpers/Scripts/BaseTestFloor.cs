using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Internals.Floors;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using Base_CityGeneration.Elements.Generic;
using EpimetheusPlugins.Scripts;
using SwizzleMyVectors;

namespace Base_CityGeneration.TestHelpers.Scripts
{
    public class BaseTestFloor
        : BaseFloor
    {
        private readonly IReadOnlyList<IReadOnlyList<Vector2>> _rooms;

        public BaseTestFloor(params IReadOnlyList<Vector2>[] rooms)
        {
            _rooms = rooms;
        }

        protected override IEnumerable<KeyValuePair<VerticalSelection, IRoomPlan>> CreateFloorPlan(IFloorPlanBuilder builder, IReadOnlyDictionary<IRoomPlan, KeyValuePair<VerticalSelection, IVerticalFeature>> overlappingVerticalElements, IReadOnlyList<ConstrainedVerticalSelection> constrainedVerticalElements)
        {
            if (constrainedVerticalElements.Count != 0)
                throw new InvalidOperationException("Base Test Floor does not support vertical elements");

            foreach (var room in _rooms)
            {
                var shape = room.Select(a => Vector3.Transform(a.X_Y(0), InverseWorldTransformation).XZ()).ToArray();

                var plan = builder.Add(shape, 0.1f).Single();
                plan.AddScript(1, new ScriptReference(typeof(BlankRoom)));
            }

            return new KeyValuePair<VerticalSelection, IRoomPlan>[0];
        }
    }
}

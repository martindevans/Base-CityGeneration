using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Floors;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;

namespace Base_CityGeneration.TestHelpers.Scripts
{
    [Script("07880B82-DD78-422D-9230-61101283CE83", "Blank Test Floor")]
    public class BlankTestFloor
        : BaseFloor
    {
        public IEnumerable<KeyValuePair<VerticalSelection, IVerticalFeature>> OverlappingVerticals { get; private set; }

        protected override IConfigurableFacade FailedToFindExternalSection(IRoomPlan roomPlan, Facade facade)
        {
            return null;
        }

        protected override IEnumerable<KeyValuePair<VerticalSelection, IRoomPlan>> CreateFloorPlan(IFloorPlanBuilder builder, IReadOnlyDictionary<IRoomPlan, KeyValuePair<VerticalSelection, IVerticalFeature>> overlappingVerticalElements, IReadOnlyList<ConstrainedVerticalSelection> constrainedVerticalElements)
        {
            OverlappingVerticals = overlappingVerticalElements.Select(a => a.Value).ToArray();

            return from el in constrainedVerticalElements
                   let f = el.ConstrainedFootprint.Shrink(0.5f) 
                   from r in builder.Add(f, 0.1f)
                   select new KeyValuePair<VerticalSelection, IRoomPlan>(el, r);
        }
    }
}

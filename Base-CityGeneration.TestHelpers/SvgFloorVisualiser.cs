using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using PrimitiveSvgBuilder;

namespace Base_CityGeneration.TestHelpers
{
    public static class SvgRoomVisualiser
    {
        public static string FloorplanToSvg(IFloorPlanBuilder plan, float scalePosition = 1, bool basic = false)
        {
            var builder = new SvgBuilder(scalePosition);
            builder.Outline(plan.ExternalFootprint, "black", "rgba(10, 10, 10, 0.25)");

            foreach (var room in plan.Rooms)
                builder.Outline(room.OuterFootprint, fill: "cornflowerblue");

            foreach (var room in plan.Rooms)
            {
                if (basic)
                {
                    builder.Outline(room.InnerFootprint, fill: "lightsteelblue");
                }
                else
                {
                    var walls = room.GetWalls();

                    foreach (var facade in walls)
                    {
                        builder.Outline(new[] {
                            facade.Section.Inner1, facade.Section.Inner2, facade.Section.Outer1, facade.Section.Outer2
                        }, facade.IsExternal ? "yellow" : "blue", "dimgray");
                    }

                    var corners = room.GetCorners();

                    foreach (var corner in corners)
                    {
                        builder.Outline(corner, "royalblue", "navyblue");
                    }
                }
            }

            return builder.ToString();
        }
    }
}

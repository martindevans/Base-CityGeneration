using System;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using PrimitiveSvgBuilder;

namespace Base_CityGeneration.TestHelpers
{
    public static class SvgRoomVisualiser
    {
        public static string FloorplanToSvg(IFloorPlanBuilder plan, float scalePosition = 1)
        {
            var rand = new Random();

            var builder = new SvgBuilder(scalePosition);
            builder.Outline(plan.ExternalFootprint, "black", "rgba(10, 10, 10, 0.25)");

            foreach (var room in plan.Rooms)
            {
                foreach (var facade in room.GetWalls())
                {
                    string color = "blue";
                    if (facade.IsExternal && facade.Section.IsCorner)
                        color = "purple";
                    else if (facade.IsExternal)
                        color = "green";
                    else if (facade.Section.IsCorner)
                        color = "cornflowerblue";
                    else if (facade.NeighbouringRoom != null)
                        color = string.Format("rgb({0},{1},{2})", rand.Next(100, 255), rand.Next(50), rand.Next(50));

                    builder.Outline(new[] {
                        facade.Section.A, facade.Section.B, facade.Section.C, facade.Section.D
                    }, color, "grey");
                }

                //builder.Outline(room.OuterFootprint, fill: "red");
            }

            return builder.ToString();
        }
    }
}

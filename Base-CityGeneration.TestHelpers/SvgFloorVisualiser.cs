using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.TestHelpers
{
    public abstract class SvgRoomVisualiser
    {
        public string GetSvgForFloorplan(FloorPlan plan)
        {
            return FloorplanToSvg(plan).ToString();
        }

        public static XElement FloorplanToSvg(FloorPlan plan)
        {
            const float scale = 2f;
            Random rand = new Random();

            List<XObject> paths = new List<XObject>
            {
                ToSvgPath(plan.ExternalFootprint, "black", scale: scale, fill:"grey", opacity: 0.1f)
            };

            //Add Rooms
            foreach (var r in plan.Rooms.Select((a, i) => new { room = a, i }).Skip(0).Take(1000))
            {
                var room = r.room;

                //Sections
                foreach (var facade in room.GetFacades())
                {
                    string c = "blue";
                    if (facade.IsExternal && facade.Section.IsCorner)
                        c = "purple";
                    else if (facade.IsExternal)
                        c = "green";
                    else if (facade.Section.IsCorner)
                        c = "cornflowerblue";
                    else if (facade.NeighbouringRoom != null)
                        c = string.Format("rgb({0},{1},{2})", rand.Next(100, 255), rand.Next(50), rand.Next(50));

                    paths.Add(ToSvgPath(new[] { facade.Section.A, facade.Section.B, facade.Section.C, facade.Section.D }, c, scale: scale, fill: "none"));
                }
            }

            const int w = 700;
            const int h = 700;

            paths.AddRange(new XObject[]
            {
                new XAttribute("transform", string.Format("translate({0},{1}) scale(1,-1)", w/2, h/2))
            });

            List<XObject> svgContents = new List<XObject>
            {
                new XAttribute("height", h),
                new XAttribute("width", w),
                new XElement("g", paths)
            };
            return new XElement("svg", svgContents);
        }

        private static XElement ToSvgPath(IEnumerable<Vector2> points, string stroke, float scale = 1, string fill = "white", float opacity = 0.75f)
        {
            points = points.ToArray().Select(a => a * scale);

            var d = String.Format("M{0} {1}", points.First().X, points.First().Y) +
                String.Join(" ", points.Select(p => string.Format("L{0} {1}", p.X, p.Y))) + " Z";

            return new XElement("path",
                new XAttribute("d", d),
                new XAttribute("stroke", stroke),
                new XAttribute("fill", fill),
                new XAttribute("opacity", opacity),
                new XAttribute("stroke-width", stroke));
        }
    }

    public interface IFloorTester
    {
        void CreateRooms(FloorPlan plan);
    }
}

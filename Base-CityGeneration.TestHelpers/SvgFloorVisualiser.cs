using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan.Geometric;

namespace Base_CityGeneration.TestHelpers
{
    public static class SvgRoomVisualiser
    {
        public static XElement FloorNodeToSvg(IFloorTester tester, params Vector2[] footprint)
        {
            var plan = new GeometricFloorplan(new ReadOnlyCollection<Vector2>(footprint));
            tester.CreateRooms(plan);

            return FloorplanToSvg(plan);
        }

        public static XElement FloorplanToSvg(IFloorPlanBuilder plan, float scalePosition = 1)
        {
            var rand = new Random();

            var elements = new List<XObject>
            {
                ToSvgPath(plan.ExternalFootprint.Select(a => a * scalePosition), "black", fill:"grey", opacity: 0.1f)
            };

            //Add Rooms
            foreach (var r in plan.Rooms.Select((a, i) => new { room = a, i }).Skip(0).Take(1000))
            {
                var room = r.room;

                //Sections
                foreach (var facade in room.GetWalls())
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

                    elements.Add(ToSvgPath(new[] { facade.Section.A * scalePosition, facade.Section.B * scalePosition, facade.Section.C * scalePosition, facade.Section.D * scalePosition }, c, fill: "none"));
                }

                //var scr = room.Scripts.FirstOrDefault();
                //if (scr != null)
                //    elements.Add(ToSvgText((room.InnerFootprint.Aggregate((a, b) => a + b) / room.InnerFootprint.Count) * scalePosition, scr.Name, fontSize: 7));
            }

            const int w = 1000;
            const int h = 700;

            elements.AddRange(new XObject[]
            {
                new XAttribute("transform", string.Format("translate({0},{1}) scale(2,-2)", w/2, h/2))
            });

            var svgContents = new List<XObject>
            {
                new XAttribute("height", h),
                new XAttribute("width", w),
                new XElement("g", elements)
            };
            return new XElement("svg", svgContents);
        }

        private static XObject ToSvgText(Vector2 position, string text, string fontFamily = "verdana", float fontSize = 20)
        {
            //plain text element
            var txt = new XElement("text",
                new XAttribute("font-family", fontFamily),
                new XAttribute("font-size", fontSize),
                new XAttribute("text-anchor", "middle"),
                text
            );

            //because the entire diagram is inverted, we must uninvert the text
            return new XElement("g",
                new XAttribute("transform", string.Format("scale(1, -1), translate({0}, {1})", position.X, -position.Y)),
                txt
            );
        }

        private static XElement ToSvgPath(IEnumerable<Vector2> points, string stroke, string fill = "white", float opacity = 0.75f)
        {
            var d = string.Format("M{0} {1}", points.First().X, points.First().Y) +
                string.Join(" ", points.Select(p => string.Format("L{0} {1}", p.X, p.Y))) + " Z";

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
        void CreateRooms(IFloorPlanBuilder plan);
    }
}

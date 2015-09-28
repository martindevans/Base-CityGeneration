using EpimetheusPlugins.Procedural.Utilities;
using SwizzleMyVectors;
using System;
using System.Linq;
using System.Numerics;

namespace Base_CityGeneration.Elements.Building.Design.Spec.FacadeConstraints
{
    public class AccessConstraint
        : BaseFacadeConstraint
    {
        public string Type { get; private set; }

        private AccessConstraint(string type)
        {
            Type = type;
        }

        public override bool Check(FloorSelection floor, BuildingSideInfo[] sides, Vector2 edgeStart, Vector2 edgeEnd, float bottom, float top)
        {
            //Direction of the edge of the building
            var eDir = Vector2.Normalize(edgeEnd - edgeStart);

            foreach (var side in sides)
            {
                var sideLine = new Line2D(side.EdgeStart, side.EdgeEnd - side.EdgeStart);

                //Project out edgeStart/edgeEnd perpendicular and convert to distance along edge
                var iStart = Geometry2D.LineLineIntersection(new Line2D(edgeStart, eDir.Perpendicular()), sideLine);
                var iEnd = Geometry2D.LineLineIntersection(new Line2D(edgeEnd, eDir.Perpendicular()), sideLine);

                //No intersections means we have nothing to do with this neighbour
                if (!iStart.HasValue || !iEnd.HasValue)
                    continue;

                //Extract start and end distances along side
                var st = Math.Min(iStart.Value.DistanceAlongLineB, iEnd.Value.DistanceAlongLineB);
                var et = Math.Max(iStart.Value.DistanceAlongLineB, iEnd.Value.DistanceAlongLineB);

                //We can select a subsection of the neighbour edge (distances along edge B)
                //Check if all of the neighbours along that subsection have the resource we're looking for
                foreach (var neighbour in side.Neighbours)
                {
                    //Skip this subsection if it does not overlap our area of interest
                    if (neighbour.Start > et || neighbour.End < st)
                        continue;

                    //Try to find the resource we want
                    bool resource = neighbour.Resources.Any(r => r.Bottom <= bottom && r.Top >= top && r.Type == Type);

                    //If we did not find the resource, then this entire section does not have the resource (obviously) so we have failed
                    if (!resource)
                        return false;
                }
            }

            //We worked through all applicable edges, and did not find the resource lacking along any of them: success!
            return true;
        }

        internal class Container
            : BaseContainer
        {
            public string Type { get; set; }

            public override BaseFacadeConstraint Unwrap()
            {
                return new AccessConstraint(Type);
            }
        }
    }
}

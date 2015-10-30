using System;
using System.Linq;
using EpimetheusPlugins.Procedural.Utilities;
using SwizzleMyVectors;
using System.Numerics;

namespace Base_CityGeneration.Elements.Building.Design.Spec.FacadeConstraints
{
    /// <summary>
    /// Check that a facade has a minimum clearance of open space to a neighbour
    /// </summary>
    public class ClearanceConstraint
        : BaseFacadeConstraint
    {
        public float Clearance { get; private set; }

        public ClearanceConstraint(float clearance)
        {
            Clearance = clearance;
        }

        public override bool Check(FloorSelection floor, BuildingSideInfo[] neighbours, Vector2 edgeStart, Vector2 edgeEnd, float bottom, float top)
        {
            return Check(Clearance, floor, neighbours, edgeStart, edgeEnd);
        }

        private static bool Check(float distance, FloorSelection floor, BuildingSideInfo[] sides, Vector2 edgeStart, Vector2 edgeEnd)
        {
            //Direction of the edge of the building
            var eDir = Vector2.Normalize(edgeEnd - edgeStart);

            foreach (var side in sides)
            {
                //No point checking sides with no neighbours!
                if (!side.Neighbours.Any())
                    continue;

                var sideLine = new Line2D(side.EdgeStart, side.EdgeEnd - side.EdgeStart);

                //Project out edgeStart/edgeEnd perpendicular and convert to distance along edge
                var iStart = Geometry2D.LineLineIntersection(new Line2D(edgeStart, -eDir.Perpendicular()), sideLine);
                var iEnd = Geometry2D.LineLineIntersection(new Line2D(edgeEnd, -eDir.Perpendicular()), sideLine);

                //No intersections means we can't possibly be obscured by this neighbour
                if (!iStart.HasValue || !iEnd.HasValue)
                    continue;

                //If the intersection is to the wrong side of this line, skip it
                if (iStart.Value.DistanceAlongLineA < 0 || iEnd.Value.DistanceAlongLineA < 0)
                    continue;

                //Extract start and end distances along side
                var st = Math.Min(iStart.Value.DistanceAlongLineB, iEnd.Value.DistanceAlongLineB);
                var et = Math.Max(iStart.Value.DistanceAlongLineB, iEnd.Value.DistanceAlongLineB);

                //We can select a subsection of the neighbour edge (distances along edge B)
                //Check if any of the buildings along that subsection break the clearance constraint
                foreach (var neighbour in side.Neighbours)
                {
                    //Skip this subsection if it is too low to have any impact on this floor
                    if (neighbour.Height <= floor.CompoundHeight)
                        continue;

                    var ns = Math.Min(neighbour.Start, neighbour.End);
                    var ne = Math.Max(neighbour.Start, neighbour.End);

                    //Skip this subsection if it does not overlap our area of interest
                    if (ns > et || ne < st)
                        continue;

                    //Distance from the start point of this neighbour, to the closest point on the edge of this facade section
                    bool cont;
                    var startClear = MeasureClearance(out cont, sideLine, sideLine.Point + sideLine.Direction * ns, new LineSegment2D(edgeStart, edgeEnd), eDir);
                    if (cont)
                        continue;

                    //Distance from the end point of this neighbour, to the closest point on the edge of this facade section
                    var endClear = MeasureClearance(out cont, sideLine, sideLine.Point + sideLine.Direction * ne, new LineSegment2D(edgeStart, edgeEnd), eDir);
                    if (cont)
                        continue;

                    //Check clearance
                    if (endClear < distance || startClear < distance)
                        return false;
                }
            }

            return true;
        }

        private static float MeasureClearance(out bool skip, Line2D sideLine, Vector2 point, LineSegment2D edgeSeg, Vector2 edgeDir)
        {
            var startSegPoint = Geometry2D.ClosestPointOnLineSegment(edgeSeg, point);
            var sidePoint = Geometry2D.LineLineIntersection(new Line2D(startSegPoint, edgeDir.Perpendicular()), sideLine);

            if (!sidePoint.HasValue)
            {
                skip = true;
                return 0;
            }

            skip = false;
            return sidePoint.Value.DistanceAlongLineA;
        }

        internal class Container
            : BaseContainer
        {
            public float Distance { get; set; }

            public override BaseFacadeConstraint Unwrap()
            {
                return new ClearanceConstraint(Distance);
            }
        }
    }
}

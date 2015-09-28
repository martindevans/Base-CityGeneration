using System;
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

        private ClearanceConstraint(float clearance)
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
                var sideLine = new Line2D(side.EdgeStart, side.EdgeEnd - side.EdgeStart);
                var edgeLine = new Line2D(edgeStart, eDir);

                //Project out edgeStart/edgeEnd perpendicular and convert to distance along edge
                var iStart = Geometry2D.LineLineIntersection(new Line2D(edgeStart, eDir.Perpendicular()), sideLine);
                var iEnd = Geometry2D.LineLineIntersection(new Line2D(edgeEnd, eDir.Perpendicular()), sideLine);

                //No intersections means we can't possibly be obscured by this neighbour
                if (!iStart.HasValue || !iEnd.HasValue)
                    continue;

                //Extract start and end distances along side
                var st = Math.Min(iStart.Value.DistanceAlongLineB, iEnd.Value.DistanceAlongLineB);
                var et = Math.Max(iStart.Value.DistanceAlongLineB, iEnd.Value.DistanceAlongLineB);

                //We can select a subsection of the neighbour edge (distances along edge B)
                //Check if any of the buildings along that subsection break the clearance constraint
                foreach (var neighbour in side.Neighbours)
                {
                    //Skip this subsection if it does not overlap our area of interest
                    if (neighbour.Start > et || neighbour.End < st)
                        continue;

                    //Skip this subsection if it is too low to have any impact on this floor
                    if (neighbour.Height <= floor.CompoundHeight)
                        continue;

                    //Distance from the start point of this neighbour, to the closest point on the edge of this facade section
                    var nStart = sideLine.Point + sideLine.Direction * neighbour.Start;
                    var startClear = Vector2.Distance(nStart, edgeLine.Point + edgeLine.Direction * Geometry2D.ClosestPointDistanceAlongLine(edgeLine, nStart));

                    //Distance from the end point of this neighbour, to the closest point on the edge of this facade section
                    var nEnd = sideLine.Point + sideLine.Direction * neighbour.End;
                    var t = Geometry2D.ClosestPointDistanceAlongLine(edgeLine, nEnd);
                    var endClear = Vector2.Distance(nEnd, edgeLine.Point + edgeLine.Direction * t);

                    //Check clearance
                    if (endClear < distance || startClear < distance)
                        return false;
                }
            }

            return true;
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

using System.Collections.Generic;
using System.Numerics;
using Base_CityGeneration.Datastructures;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    internal class FloorplanRegion
    {
        private readonly IReadOnlyList<Vector2> _shape;
        public IReadOnlyList<Vector2> Shape
        {
            get { return _shape; }
        }

        private readonly BoundingRectangle _bounds;
        public BoundingRectangle Bounds
        {
            get { return _bounds; }
        }

        private readonly OABR _oabb;
        public OABR OABB
        {
            get { return _oabb; }
        }

        public FloorplanRegion(IReadOnlyList<Vector2> shape)
        {
            _shape = shape;
            _bounds = BoundingRectangle.CreateFromPoints(shape);

            _oabb = OABR.Fit(shape);
        }

        #region neighbourhood
        //public NeighbourIndex? IsNeighbour(FloorplanRegion region)
        //{
        //    for (var i = 0; i < _shape.Count; i++)
        //    {
        //        var a = _shape[i];
        //        var b = _shape[(i + 1) % _shape.Count];

        //        for (var j = 0; j < region.Shape.Count; j++)
        //        {
        //            var x = region.Shape[j];
        //            var y = region.Shape[(j + 1) % region.Shape.Count];

        //            if (Vector2.DistanceSquared(x, b) < 0.01f && Vector2.DistanceSquared(y, a) < 0.01f)
        //                return new NeighbourIndex(this, i, region, j);
        //        }
        //    }

        //    return null;
        //}

        //public IEnumerable<NeighbourIndex> FindNeighbours(Quadtree<KeyValuePair<FloorplanRegion, float>> regions)
        //{
        //    return regions
        //        .Intersects(new BoundingRectangle(Bounds.Min - new Vector2(0.5f), Bounds.Max + new Vector2(0.5f)))
        //        .Select(a => a.Key)
        //        .Where(candidate => !ReferenceEquals(this, candidate))
        //        .Select(IsNeighbour)
        //        .Where(a => a.HasValue)
        //        .Select(a => a.Value);
        //}

        //public struct NeighbourIndex
        //    : IEquatable<NeighbourIndex>
        //{
        //    public FloorplanRegion Region { get; private set; }
        //    public int RegionOverlapIndex { get; private set; }

        //    public FloorplanRegion Neighbour { get; private set; }
        //    public int NeighbourOverlapIndex { get; private set; }

        //    public NeighbourIndex(FloorplanRegion region, int regionOverlapIndex, FloorplanRegion neighbour, int neighbourOverlapIndex)
        //        : this()
        //    {
        //        RegionOverlapIndex = regionOverlapIndex;
        //        Region = region;
        //        Neighbour = neighbour;
        //        NeighbourOverlapIndex = neighbourOverlapIndex;
        //    }

        //    public bool Equals(NeighbourIndex other)
        //    {
        //        return ReferenceEquals(other.Neighbour, Neighbour)
        //            && ReferenceEquals(other.Region, Region)
        //            && other.NeighbourOverlapIndex.Equals(NeighbourOverlapIndex)
        //            && other.RegionOverlapIndex.Equals(RegionOverlapIndex);
        //    }
        //}

        //public static FloorplanRegion Merge(NeighbourIndex merge)
        //{
        //    //Implementation note: https://onedrive.live.com/redir?resid=A09C9578613251A0!59568&authkey=!AEwuTix9nt7VfKU&ithint=onenote%2c

        //    //Target to copy vertices into
        //    var vertices = new List<Vector2>(merge.Region.Shape.Count + merge.Neighbour.Shape.Count - 2);

        //    //Copy from 0 -> RegionVertexIndex
        //    for (var i = 0; i < merge.RegionOverlapIndex; i++)
        //        vertices.Add(merge.Region.Shape[i]);

        //    //Copy from (merge.NeighbourVertexIndex + 1) -> merge.NeighbourVertexIndex (with circular indexing)
        //    for (var i = 0; i < merge.Neighbour.Shape.Count; i++)
        //        vertices.Add(merge.Neighbour.Shape[(i + merge.NeighbourOverlapIndex + 1) % merge.Neighbour.Shape.Count]);

        //    //Copy from RegionVertexIndex -> 9 (with circular indexing)
        //    for (var i = merge.RegionOverlapIndex + 2; i < merge.Region.Shape.Count; i++)
        //        vertices.Add(merge.Region.Shape[i % merge.Region.Shape.Count]);

        //    //We've merged the single face these vertices touch at, but that might not be sufficient if they touch at multiple faces
        //    //We need to find runs of vertices which go A -> B -> A and unwind them (remove A -> B)
        //    bool end = false;
        //    while (!end)
        //    {
        //        for (var i = vertices.Count - 1; i >= 0; i--)
        //        {
        //            var ai = i;
        //            var a = vertices[ai];

        //            var bi = (i + 1) % vertices.Count;

        //            var ci = (i + 2) % vertices.Count;
        //            var c = vertices[ci];

        //            if (a.Equals(c))
        //            {
        //                vertices.RemoveAt(ci);
        //                vertices.RemoveAt(bi);
        //                goto restart;
        //            }
        //        }

        //        //If we get here it means we exited the inner loop naturally, job done!
        //        end = true;

        //        //Skip out of the inner loop and start again
        //        restart:;
        //    }

        //    //Done!
        //    return new FloorplanRegion(vertices);
        //}
        #endregion
    }
}

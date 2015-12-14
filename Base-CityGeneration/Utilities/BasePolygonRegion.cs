using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Datastructures;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural.Utilities;
using SwizzleMyVectors;
using SwizzleMyVectors.Geometry;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Base_CityGeneration.Utilities
{
    /// <summary>
    /// Base class for regions of space defined by a polygon
    /// </summary>
    public abstract class BasePolygonRegion<TSelf>
        where TSelf : BasePolygonRegion<TSelf>
    {
        #region fields and properties
        private readonly IReadOnlyList<Vector2> _shape;
        /// <summary>
        /// The shape of this region
        /// </summary>
        public IReadOnlyList<Vector2> Shape
        {
            get { return _shape; }
        }

        private readonly BoundingRectangle _bounds;
        /// <summary>
        /// The AABB of this region
        /// </summary>
        public BoundingRectangle Bounds
        {
            get { return _bounds; }
        }

        private readonly OABR _oabr;
        /// <summary>
        /// The best fit OABB of this region
        /// </summary>
        public OABR OABR
        {
            get { return _oabr; }
        }
        #endregion

        #region construction
        protected BasePolygonRegion(IReadOnlyList<Vector2> shape, OABR oabr)
        {
            _shape = shape;

            _bounds = BoundingRectangle.CreateFromPoints(shape);
            _oabr = oabr;
        }

        protected abstract TSelf Construct(IReadOnlyList<Vector2> shape, OABR oabr);

        public TSelf Construct(IReadOnlyList<Vector2> shape)
        {
            return Construct(shape, OABR.Fit(shape));
        }
        #endregion

        #region error reduction
        /// <summary>
        /// Reduce error until it is below tolerance (recursively for all results)
        /// </summary>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public IEnumerable<TSelf> RecursiveReduceError(float tolerance)
        {
            var result = new List<TSelf>();

            if (ReduceError(tolerance, result))
            {
                //This shape was reduced, recurse to reduce error of the results
                return (from shape in result
                        from reduced in shape.RecursiveReduceError(tolerance)
                        select reduced).ToArray();
            }
            else
            {
                //shape was unchanged, base case - just return
                return result;
            }
        }

        /// <summary>
        /// Subdivide this region to reduce the areal difference between the OABR and the shape
        /// </summary>
        /// <param name="tolerance">If error is below this tolerance then nothing will happen</param>
        /// <returns></returns>
        public IEnumerable<TSelf> ReduceError(float tolerance)
        {
            var result = new List<TSelf>();
            ReduceError(tolerance, result);
            return result;
        }

        private bool ReduceError(float tolerance, List<TSelf> result)
        {
            //Base case: error is acceptable
            if (MeasureOabrError() < tolerance)
            {
                result.Add((TSelf)this);
                return false;
            }

            //There are different split strategies for convex and concave regions (notes here: https://onedrive.live.com/view.aspx?cid=a09c9578613251a0&id=documents&resid=A09C9578613251A0%2159568&app=OneNote&authkey=!AEwuTix9nt7VfKU&&wd=target%28%2F%2FDated%20notes.one%7Ce28ecbf1-86ee-4c11-9c60-b67ed104b40a%2F2015-12-08%7C7ddfd868-4b67-417b-ba7a-c46b46e504fe%2F%29)


            //Find the most concave corner
            var index = -1;
            var turn = float.PositiveInfinity;
            for (var i = 0; i < _shape.Count; i++)
            {
                //Get three points forming a corner
                var a = _shape[i];
                var b = _shape[(i + 1) % _shape.Count];
                var c = _shape[(i + 2) % _shape.Count];

                //Get vectors into and out of corner
                var ab = b - a;
                var bc = c - b;

                //Cross product to determine if this is an interior or exterior turn
                var cross = ab.Cross(bc);

                //If it's not a concave turn we don't care
                if (Math.Sign(cross) <= 0)
                    continue;

                //Calculate the angle of the concave turn
                var angle = MathHelper.Pi - (float)Math.Acos(Vector2.Dot(ab, bc));

                //Do we beat the best yet?
                if (angle < turn)
                {
                    turn = angle;
                    index = i;
                }
            }

            //Check if this is a concave or convex shape
            if (index == -1)
            {
                //Concave (no convex corners found) - Slice down the center of the short axis
                var parts = _shape.SlicePolygon(new Ray2(_oabr.Middle, _oabr.SplitDirection()));
                result.AddRange(parts.Select(Construct));
            }
            else
            {
                //Slice this shape along the most concave split line

                //Get three points forming the corner
                var a = _shape[index];
                var b = _shape[(index + 1) % _shape.Count];
                var c = _shape[(index + 2) % _shape.Count];

                //Get vectors into and out of corner
                var ab = b - a;
                var bc = c - b;

                //Slice along both the lines
                var slicedAB = _shape.SlicePolygon(new Ray2(b, ab)).Select(Construct);
                var slicedBC = _shape.SlicePolygon(new Ray2(b, bc)).Select(Construct);

                //Select the largest error in each set
                var abMin = slicedAB.Select(x => MeasureOabrError(x.Shape, x.OABR)).Max();
                var bcMin = slicedBC.Select(x => MeasureOabrError(x.Shape, x.OABR)).Max();
                var best = abMin <= bcMin ? slicedAB : slicedBC;

                //Output the set with the smallest largest error
                result.AddRange(best);
            }

            return true;
        }

        private float MeasureOabrError()
        {
            return MeasureOabrError(_shape, _oabr);
        }

        private static float MeasureOabrError(IReadOnlyList<Vector2> shape, OABR bounds)
        {
            var area = shape.Area();
            var rectArea = bounds.Area;

            return rectArea - area;
        }
        #endregion
    }
}

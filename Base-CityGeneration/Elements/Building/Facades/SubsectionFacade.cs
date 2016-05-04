using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Geometry.Walls;
using EpimetheusPlugins.Procedural;
using SwizzleMyVectors.Geometry;
using MathHelperRedux;

namespace Base_CityGeneration.Elements.Building.Facades
{
    /// <summary>
    /// Adds stamps to a smaller section of a parent facade (ensures that stamps added to this subsection do not overhang)
    /// </summary>
    public class SubsectionFacade
        : IConfigurableFacade
    {
        private readonly IConfigurableFacade _parent;

        private readonly Vector2 _delta;
        private readonly Vector2 _maxXY;
        private readonly Vector2 _minXY;

        private readonly float _rangeDepth;
        private readonly float _minDepth;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent">The parent facade to add stamps to</param>
        /// <param name="min">The minimum coordinate on the parent</param>
        /// <param name="max">The maximum coordinate on the parent</param>
        /// <param name="depthMin">The minimum depth on the parent</param>
        /// <param name="depthMax">The maximum depth on the parent</param>
        /// <param name="section"></param>
        public SubsectionFacade(IConfigurableFacade parent, Vector2 min, Vector2 max, float depthMin, float depthMax, Section section)
        {
            Contract.Requires(parent != null);

            _parent = parent;

            _delta = (min + max) / 2;
            _minXY = min;
            _maxXY = max;

            _rangeDepth = depthMax - depthMin;
            _minDepth = depthMin;

            Section = section;
        }

        public IEnumerable<BaseFacade.Stamp> Stamps
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<BaseFacade.Stamp>>() != null);

                //Transform stamp from parent space into subsection space, discard stamps which do not overlap this subsection
                return from stamp in _parent.Stamps

                       //Remap from parent space into subsection space
                       let remapped = new BaseFacade.Stamp(
                           FromParentDepth(stamp.StartDepth), FromParentDepth(stamp.EndDepth),
                           stamp.Additive, stamp.Material,
                           stamp.Shape.Select(FromParentXY).ToArray()
                       )

                       //Check stamp intersects subsection (in depth)
                       where Math.Min(remapped.StartDepth, remapped.EndDepth) <= (_minDepth + _rangeDepth)
                       where Math.Max(remapped.StartDepth, remapped.EndDepth) >= _minDepth

                       //Calculate bounding box of stamp (in subsection space)
                       let xs = (from xy in remapped.Shape select xy.X)
                       let minX = xs.Min()
                       let maxX = xs.Max()
                       let ys = (from xy in remapped.Shape select xy.Y)
                       let minY = ys.Min() 
                       let maxY = ys.Max()
                       let bounds = new BoundingRectangle(new Vector2(minX, minY), new Vector2(maxX, maxY))

                       //Check that the stamp intersects the subsection
                       where bounds.Intersects(new BoundingRectangle(Vector2.Zero, _maxXY - _minXY))

                       //Select this stamp
                       select remapped;
            }
        }

        public void AddStamp(BaseFacade.Stamp stamp)
        {
            _parent.AddStamp(new BaseFacade.Stamp(
                ToParentDepth(stamp.StartDepth), ToParentDepth(stamp.EndDepth),
                stamp.Additive, stamp.Material,
                stamp.Shape.Select(ToParentXY).ToArray()
            ));
        }

        public ISubdivisionContext GetDependencyContext()
        {
            return _parent.GetDependencyContext();
        }

        private float ToParentDepth(float depth)
        {
            return depth * _rangeDepth + _minDepth;
        }

        private float FromParentDepth(float parentDepth)
        {
            return (parentDepth - _minDepth) / _rangeDepth;
        }

        private Vector2 ToParentXY(Vector2 xy)
        {
            var remapped = xy + _delta;

            //Clamp to subsection to ensure that subsection stamps cannot overhang the subsection
            return new Vector2(
                MathHelper.Clamp(remapped.X, _minXY.X, _maxXY.X),
                MathHelper.Clamp(remapped.Y, _minXY.Y, _maxXY.Y)
            );
        }

        private Vector2 FromParentXY(Vector2 parentXY)
        {
            //no clamping required
            return parentXY - _delta;
        }

        public Section Section { get; set; }
    }
}

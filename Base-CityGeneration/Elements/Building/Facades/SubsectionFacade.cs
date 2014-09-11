using System;
using System.Collections.Generic;
using System.Linq;
using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.Xna.Framework;

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
        public SubsectionFacade(IConfigurableFacade parent, Vector2 min, Vector2 max, float depthMin, float depthMax, Walls.Section section)
        {
            _parent = parent;

            _delta = max - (max - min) / 2;
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
                throw new NotImplementedException("Project stamps from parent back into local space of this subsection");
                //Remember to discard stamps which do not fit within this subsection!
                //Or maybe we should keep the overhanging stamps? They may intersect this section even if they aren't entirely contained
                //Perhaps we should clip them to this seubsection, that seems a little excessive though
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

        private float ToParentDepth(float depth)
        {
            return depth * _rangeDepth + _minDepth;
        }

        private Vector2 ToParentXY(Vector2 xy)
        {
            var remapped = xy + _delta;

            return new Vector2(
                MathHelper.Clamp(remapped.X, _minXY.X, _maxXY.X),
                MathHelper.Clamp(remapped.Y, _minXY.Y, _maxXY.Y)
            );
        }

        public Walls.Section Section { get; set; }

        public void Delete()
        {
            //Make a really massive stamp, which will be clamped to the size of this subsection (thus removing the entire subsection)
            AddStamp(new BaseFacade.Stamp(0, 1, false, null, new[]
            {
                new Vector2(-float.MaxValue, -float.MaxValue),
                new Vector2(float.MaxValue, -float.MaxValue),
                new Vector2(float.MaxValue, float.MaxValue),
                new Vector2(-float.MaxValue, float.MaxValue),
            }));
        }
    }
}

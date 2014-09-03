using System;
using System.Collections.Generic;
using System.Linq;
using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Building.Facades
{
    public class SubsectionFacade
        : IConfigurableFacade
    {
        private readonly IConfigurableFacade _parent;

        private readonly Vector2 _delta;
        private readonly Vector2 _maxXY;
        private readonly Vector2 _minXY;

        private readonly float _rangeDepth;
        private readonly float _minDepth;

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
            get { throw new NotImplementedException(); }
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
    }
}

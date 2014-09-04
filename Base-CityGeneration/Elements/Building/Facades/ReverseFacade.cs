using System.Collections.Generic;
using System.Linq;
using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Building.Facades
{
    /// <summary>
    /// Add stamps to the backside of a facade (reverse depth, flip X coordinates)
    /// </summary>
    public class ReverseFacade
        : IConfigurableFacade
    {
        private readonly IConfigurableFacade _parent;

        public ReverseFacade(IConfigurableFacade parent, Walls.Section section)
        {
            _parent = parent;
            Section = section;
        }

        public IEnumerable<BaseFacade.Stamp> Stamps
        {
            get { throw new System.NotImplementedException(); }
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
            return 1 - MathHelper.Clamp(depth, 0, 1);
        }

        private Vector2 ToParentXY(Vector2 xy)
        {
            return new Vector2(-xy.X, xy.Y);
        }

        public Walls.Section Section { get; set; }
    }
}

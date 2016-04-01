using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using EpimetheusPlugins.Procedural.Utilities;
using System.Numerics;

using MathHelperRedux;

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
            Contract.Requires(parent != null);

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
                ToParentDepth(stamp.EndDepth), ToParentDepth(stamp.StartDepth),
                stamp.Additive, stamp.Material,
                stamp.Shape.Select(ToParentXY).ToArray()
            ));
        }

        private static float ToParentDepth(float depth)
        {
            return 1 - MathHelper.Clamp(depth, 0, 1);
        }

        private static Vector2 ToParentXY(Vector2 xy)
        {
            return new Vector2(-xy.X, xy.Y);
        }

        public Walls.Section Section { get; set; }
    }
}

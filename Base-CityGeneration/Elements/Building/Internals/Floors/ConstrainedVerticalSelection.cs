using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Base_CityGeneration.Elements.Building.Design;
using EpimetheusPlugins.Scripts;

namespace Base_CityGeneration.Elements.Building.Internals.Floors
{
    public class ConstrainedVerticalSelection
        : VerticalSelection
    {
        private readonly IReadOnlyList<Vector2> _constrainedFootprint;
        public IReadOnlyList<Vector2> ConstrainedFootprint
        {
            get { return _constrainedFootprint; }
        }

        public ConstrainedVerticalSelection(ScriptReference script, int bottom, int top, IReadOnlyList<Vector2> constrainedFootprint)
            : base(script, bottom, top)
        {
            _constrainedFootprint = constrainedFootprint;
        }

        public ConstrainedVerticalSelection(VerticalSelection vertical, IReadOnlyList<Vector2> constrainedFootprint)
            : this(vertical.Script, vertical.Bottom, vertical.Top, constrainedFootprint)
        {
        }
    }
}

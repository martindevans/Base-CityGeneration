using System.Diagnostics.Contracts;
using EpimetheusPlugins.Scripts;

namespace Base_CityGeneration.Elements.Building.Design
{
    public class VerticalSelection
    {
        private readonly ScriptReference _script;
        public ScriptReference Script
        {
            get
            {
                return _script;
            }
        }

        private readonly int _bottom;
        public int Bottom
        {
            get
            {
                return _bottom;
            }
        }

        private readonly int _top;
        public int Top
        {
            get
            {
                return _top;
            }
        }

        public VerticalSelection(ScriptReference script, int bottom, int top)
        {
            Contract.Requires(bottom < top);
            Contract.Requires(script != null);

            _script = script;
            _bottom = bottom;
            _top = top;
        }

        /// <summary>
        /// Determine which floor should "start" this vertical
        /// </summary>
        /// <returns></returns>
        public int StartingFloor()
        {
            //There are three possible cases:
            //
            // G.....B----T...
            // i.e. Bottom and top are above ground, in this case start at bottom
            //
            // ..B---T...G....
            // i.e. Bottom and top are below ground, in this case start at top
            //
            // ..B---G---T....
            // i.e. We *cross* the ground floor, in this case start at ground

            if (Bottom >= 0 && Top >= 0)
                return Bottom;
            else if (Bottom <= 0 && Top <= 0)
                return Top;
            else
                return 0;
        }
    }
}

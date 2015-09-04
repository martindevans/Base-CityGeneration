using EpimetheusPlugins.Scripts;

namespace Base_CityGeneration.Elements.Building.Design
{
    public class FacadeSelection
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

        public FacadeSelection(ScriptReference script, int bottom, int top)
        {
            _script = script;
            _bottom = bottom;
            _top = top;
        }
    }
}

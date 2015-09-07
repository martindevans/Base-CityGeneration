using Base_CityGeneration.Elements.Building.Design.Spec;
using EpimetheusPlugins.Scripts;
using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Design
{
    public class FloorSelection
    {
        private readonly string _id;
        public string Id { get { return _id; } }

        private readonly string[] _tags;
        public IEnumerable<string> Tags { get { return _tags; } }

        private readonly BaseFloorSelector _selector;
        public BaseFloorSelector Selector { get { return _selector; } }

        readonly ScriptReference _script;
        public ScriptReference Script
        {
            get
            {
                return _script;
            }
        }

        readonly float _height;
        public float Height
        {
            get
            {
                return _height;
            }
        }

        public int Index { get; internal set; }

        public FloorSelection(string id, string[] tags, BaseFloorSelector selector, ScriptReference script, float height, int index = 0)
        {
            _id = id;
            _tags = tags;
            _script = script;
            _selector = selector;
            _height = height;
            Index = index;
        }

        public FloorSelection(FloorSelection selection, int index)
        {
            _id = selection.Id;
            _tags = selection.Tags.ToArray();
            _script = selection.Script;
            _height = selection.Height;
            Index = index;
        }

        internal FloorSelection Clone()
        {
            return new FloorSelection(this, Index);
        }
    }
}

using EpimetheusPlugins.Scripts;
using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Design.Spec
{
    public class FloorSelection
    {
        private readonly string _id;
        public string Id { get { return _id; } }

        private readonly string[] _tags;
        public IEnumerable<string> Tags { get { return _tags; } }

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

        private readonly int _index;
        public int Index
        {
            get
            {
                return _index;
            }
        }

        public FloorSelection(string id, string[] tags, ScriptReference script, float height, int index = 0)
        {
            _id = id;
            _tags = tags;
            _script = script;
            _height = height;
            _index = index;
        }

        public FloorSelection(FloorSelection selection, int index)
        {
            _id = selection.Id;
            _tags = selection.Tags.ToArray();
            _script = selection.Script;
            _height = selection.Height;
            _index = index;
        }
    }
}

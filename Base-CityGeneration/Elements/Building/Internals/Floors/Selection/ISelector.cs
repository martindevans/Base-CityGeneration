
using System;
using System.Collections.Generic;
using EpimetheusPlugins.Scripts;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection
{
    public interface ISelector
    {
        IEnumerable<FloorSelection> Select(Func<double> random, Func<string[], ScriptReference> finder);
    }

    public struct FloorSelection
    {
        private readonly string _id;
        public string Id { get { return _id; } }

        private readonly string[] _tags;
        public string[] Tags { get { return _tags; } }

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

        public FloorSelection(string id, string[] tags, ScriptReference script, float height)
            : this()
        {
            _id = id;
            _tags = tags;
            _script = script;
            _height = height;
        }
    }

    public struct VerticalSelection
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
            _script = script;
            _bottom = bottom;
            _top = top;
        }
    }

    internal interface ISelectorContainer
    {
        ISelector Unwrap();
    }
}

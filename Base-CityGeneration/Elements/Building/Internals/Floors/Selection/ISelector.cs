
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

        private readonly int _index;
        public int Index
        {
            get
            {
                return _index;
            }
        }

        public FloorSelection(string id, string[] tags, ScriptReference script, float height, int index = 0)
            : this()
        {
            _id = id;
            _tags = tags;
            _script = script;
            _height = height;
            _index = index;
        }

        public FloorSelection(FloorSelection selection, int index)
            : this()
        {
            _id = selection.Id;
            _tags = selection.Tags;
            _script = selection.Script;
            _height = selection.Height;
            _index = index;
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

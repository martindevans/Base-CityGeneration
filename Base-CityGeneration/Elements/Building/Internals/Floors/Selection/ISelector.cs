
using System;
using System.Collections.Generic;
using EpimetheusPlugins.Scripts;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection
{
    public interface ISelector
    {
        IEnumerable<FloorSelection> Select(Func<double> random, ScriptReference[] verticals, Func<string[], ScriptReference> finder, IGroupFinder groupFinder);
    }

    public struct FloorSelection
    {
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

        public FloorSelection(ScriptReference script, float height)
            : this()
        {
            _script = script;
            _height = height;
        }
    }

    internal interface ISelectorContainer
    {
        ISelector Unwrap();
    }
}

using Base_CityGeneration.Utilities;
using EpimetheusPlugins.Scripts;
using Myre.Collections;
using System;
using System.Collections.Generic;

namespace Base_CityGeneration.Elements.Building.Design.Spec
{
    public abstract class BaseFloorSelector
    {
        public abstract IEnumerable<FloorSelection> Select(Func<double> random, INamedDataCollection metadata, Func<string[], ScriptReference> finder);

        protected FloorSelection SelectSingle(Func<double> random, IEnumerable<KeyValuePair<float, string[]>> tags, Func<string[], ScriptReference> finder, float height, string id)
        {
            string[] selectedTags;
            ScriptReference script = tags.SelectScript(random, finder, out selectedTags);
            if (script == null)
                return null;

            return new FloorSelection(id, selectedTags, this, script, height);
        }
    }

    internal interface ISelectorContainer
    {
        BaseFloorSelector Unwrap();
    }
}

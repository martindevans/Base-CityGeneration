using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Markers;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using EpimetheusPlugins.Scripts;
using Myre.Extensions;
using SharpYaml.Serialization;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection
{
    public class FloorSelector
    {
        private readonly ISelector[] _verticalSelectors;
        public IEnumerable<ISelector> VerticalSelectors
        {
            get
            {
                return _verticalSelectors;
            }
        }

        private readonly ISelector[] _floorSelectors;
        public IEnumerable<ISelector> FloorSelectors
        {
            get
            {
                return _floorSelectors;
            }
        }

        public FloorSelector(ISelector[] floorSelectors, ISelector[] verticalSelectors)
        {
            _floorSelectors = floorSelectors;
            _verticalSelectors = verticalSelectors;
        }

        public Selection Select(Func<double> random, Func<string[], ScriptReference> finder)
        {
            var verticals = SelectVerticals(random, finder, _verticalSelectors).ToArray();

            var aboveGround = _floorSelectors.TakeWhile(a => !(a is GroundMarker)).ToArray();
            var belowGround = _floorSelectors.SkipWhile(a => !(a is GroundMarker)).ToArray();

            var above = SelectFloors(random, finder, verticals, aboveGround);
            var below = SelectFloors(random, finder, verticals, belowGround);

            return new Selection(
                above.Append(below).ToArray(),
                verticals
            );
        }

        private IEnumerable<ScriptReference> SelectFloors(Func<double> random, Func<string[], ScriptReference> finder, ScriptReference[] verticals, IEnumerable<ISelector> selectors)
        {
            List<ScriptReference> floors = new List<ScriptReference>();

            foreach (var selector in selectors)
                floors.AddRange(selector.Select(random, verticals, finder));

            return floors;
        }

        private IEnumerable<ScriptReference> SelectVerticals(Func<double> random, Func<string[], ScriptReference> finder, ISelector[] verticalSelectors)
        {
            //throw new NotImplementedException();
            return new ScriptReference[0];
        }

        public struct Selection
        {
            public ScriptReference[] Floors { get; private set; }

            public ScriptReference[] Verticals { get; private set; }

            public Selection(ScriptReference[] floors, ScriptReference[] verticals)
                : this()
            {
                Floors = floors;
                Verticals = verticals;
            }
        }

        private static Serializer CreateSerializer()
        {
            var serializer = new Serializer(new SerializerSettings
            {
                EmitTags = true,
            });

            serializer.Settings.RegisterTagMapping("Ground", typeof(GroundMarker.Container));
            serializer.Settings.RegisterTagMapping("Building", typeof(Container));
            serializer.Settings.RegisterTagMapping("Floor", typeof(FloorSpec.Container));
            serializer.Settings.RegisterTagMapping("Range", typeof(FloorRangeSpec.Container));
            serializer.Settings.RegisterTagMapping("Include", typeof(FloorRangeIncludeSpec.Container));

            return serializer;
        }

        public static FloorSelector Deserialize(TextReader reader)
        {
            var s = CreateSerializer();

            return s.Deserialize<Container>(reader).Unwrap();
        }

        internal class Container
        {
            // ReSharper disable once MemberCanBePrivate.Local
            public ISelectorContainer[] Verticals { get; set; }

            // ReSharper disable once MemberCanBePrivate.Local
            public ISelectorContainer[] Floors { get; set; }

            public FloorSelector Unwrap()
            {
                return new FloorSelector(
                    Floors.Select(a => a.Unwrap()).ToArray(),
                    Verticals.Select(a => a.Unwrap()).ToArray()
                );
            }
        }
    }
}

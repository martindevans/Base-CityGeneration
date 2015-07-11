using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Markers;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Ref;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Scripts;
using HandyCollections.Extensions;
using SharpYaml.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection
{
    public class FloorSelector
    {
        private readonly VerticalElementSpec[] _verticalSelectors;
        public IEnumerable<VerticalElementSpec> VerticalSelectors
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

        public FloorSelector(ISelector[] floorSelectors, VerticalElementSpec[] verticalSelectors)
        {
            _floorSelectors = floorSelectors;
            _verticalSelectors = verticalSelectors;
        }

        public Selection Select(Func<double> random, Func<string[], ScriptReference> finder)
        {
            var aboveGround = _floorSelectors.TakeWhile(a => !(a is GroundMarker)).ToArray();
            var belowGround = _floorSelectors.SkipWhile(a => !(a is GroundMarker)).ToArray();

            //Select above ground floors, then assign indices
            var above = SelectFloors(random, finder, aboveGround).ToArray();
            for (int i = 0; i < above.Length; i++)
                above[i] = new FloorSelection(above[i], above.Length - i - 1);

            //Select below ground floors, then assign indices
            var below = SelectFloors(random, finder, belowGround).ToArray();
            for (int i = 0; i < below.Length; i++)
                below[i] = new FloorSelection(below[i], -(i + 1));

            var verticals = SelectVerticals(random, finder, _verticalSelectors, above, below).ToArray();

            return new Selection(
                above,
                below,
                verticals
            );
        }

        private static IEnumerable<FloorSelection> SelectFloors(Func<double> random, Func<string[], ScriptReference> finder, IEnumerable<ISelector> selectors)
        {
            List<FloorSelection> floors = new List<FloorSelection>();

            foreach (var selector in selectors)
                floors.AddRange(selector.Select(random, finder));

            return floors;
        }

        private static IEnumerable<VerticalSelection> SelectVerticals(Func<double> random, Func<string[], ScriptReference> finder, IEnumerable<VerticalElementSpec> verticalSelectors, IEnumerable<FloorSelection> above, IEnumerable<FloorSelection> below)
        {
            var floors = above.Append(below).ToArray();

            List<VerticalSelection> verticals = new List<VerticalSelection>();

            foreach (var selector in verticalSelectors)
                verticals.AddRange(selector.Select(random, finder, below.Count(), floors));

            return verticals;
        }

        public struct Selection
        {
            public FloorSelection[] AboveGroundFloors { get; private set; }
            public FloorSelection[] BelowGroundFloors { get; private set; }

            public VerticalSelection[] Verticals { get; private set; }

            public Selection(FloorSelection[] aboveGroundFloors, FloorSelection[] belowGroundFloors, VerticalSelection[] verticals)
                : this()
            {
                AboveGroundFloors = aboveGroundFloors;
                BelowGroundFloors = belowGroundFloors;
                Verticals = verticals;
            }
        }

        #region serialization
        private static Serializer CreateSerializer()
        {
            var serializer = new Serializer(new SerializerSettings
            {
                EmitTags = true,
            });

            //Floor element types
            serializer.Settings.RegisterTagMapping("Ground", typeof(GroundMarker.Container));
            serializer.Settings.RegisterTagMapping("Building", typeof(Container));
            serializer.Settings.RegisterTagMapping("Floor", typeof(FloorSpec.Container));
            serializer.Settings.RegisterTagMapping("Range", typeof(FloorRangeSpec.Container));
            serializer.Settings.RegisterTagMapping("Include", typeof(FloorRangeIncludeSpec.Container));
            serializer.Settings.RegisterTagMapping("Repeat", typeof(RepeatSpec.Container));

            //Utility types
            serializer.Settings.RegisterTagMapping("NormalValue", typeof(NormallyDistributedValue.Container));
            serializer.Settings.RegisterTagMapping("UniformValue", typeof(UniformlyDistributedValue.Container));

            //Ref types
            serializer.Settings.RegisterTagMapping("Num", typeof(NumRef.Container));
            serializer.Settings.RegisterTagMapping("Tagged", typeof(TaggedRef.Container));
            serializer.Settings.RegisterTagMapping("Id", typeof(IdRef.Container));

            return serializer;
        }

        public static FloorSelector Deserialize(TextReader reader)
        {
            var s = CreateSerializer();

            return s.Deserialize<Container>(reader).Unwrap();
        }

        internal class Container
        {
            public List<string> Tags { get; set; }

            public List<object> Aliases { get; set; }

            // ReSharper disable once MemberCanBePrivate.Local
            public VerticalElementSpec.Container[] Verticals { get; set; }

            // ReSharper disable once MemberCanBePrivate.Local
            public ISelectorContainer[] Floors { get; set; }

            public FloorSelector Unwrap()
            {
                return new FloorSelector(
                    Floors.Select(a => a.Unwrap()).ToArray(),
                    (Verticals ?? new VerticalElementSpec.Container[0]).Select(a => a.Unwrap()).ToArray()
                );
            }
        }
        #endregion
    }
}

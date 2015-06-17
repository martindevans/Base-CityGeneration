using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Markers;
using EpimetheusPlugins.Scripts;
using SharpYaml.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection
{
    public class FloorSelector
        : IGroupFinder
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

        private readonly KeyValuePair<string, HeightSpec>[] _heightGroups;
        public IEnumerable<KeyValuePair<string, HeightSpec>> HeightGroups
        {
            get
            {
                return _heightGroups;
            }
        }

        public FloorSelector(ISelector[] floorSelectors, ISelector[] verticalSelectors, KeyValuePair<string, HeightSpec>[] heightGroups)
        {
            _floorSelectors = floorSelectors;
            _verticalSelectors = verticalSelectors;
            _heightGroups = heightGroups;
        }

        public Selection Select(Func<double> random, Func<string[], ScriptReference> finder)
        {
            var verticals = SelectVerticals(random, finder, _verticalSelectors).ToArray();  

            var aboveGround = _floorSelectors.TakeWhile(a => !(a is GroundMarker)).ToArray();
            var belowGround = _floorSelectors.SkipWhile(a => !(a is GroundMarker)).ToArray();

            var above = SelectFloors(random, finder, verticals, aboveGround, this).ToArray();
            var below = SelectFloors(random, finder, verticals, belowGround, this).ToArray();

            return new Selection(
                above,
                below,
                verticals
            );
        }

        private IEnumerable<FloorSelection> SelectFloors(Func<double> random, Func<string[], ScriptReference> finder, ScriptReference[] verticals, IEnumerable<ISelector> selectors, IGroupFinder groupFinder)
        {
            List<FloorSelection> floors = new List<FloorSelection>();

            foreach (var selector in selectors)
                floors.AddRange(selector.Select(random, verticals, finder, groupFinder));

            return floors;
        }

        private IEnumerable<ScriptReference> SelectVerticals(Func<double> random, Func<string[], ScriptReference> finder, ISelector[] verticalSelectors)
        {
            //throw new NotImplementedException();
            return new ScriptReference[0];
        }

        public struct Selection
        {
            public FloorSelection[] AboveGroundFloors { get; private set; }
            public FloorSelection[] BelowGroundFloors { get; private set; }

            public ScriptReference[] Verticals { get; private set; }

            public Selection(FloorSelection[] aboveGroundFloors, FloorSelection[] belowGroundFloors, ScriptReference[] verticals)
                : this()
            {
                AboveGroundFloors = aboveGroundFloors;
                BelowGroundFloors = belowGroundFloors;
                Verticals = verticals;
            }
        }

        HeightSpec IGroupFinder.Find(string group)
        {
            return _heightGroups.Where(a => a.Key.Equals(group, StringComparison.InvariantCultureIgnoreCase)).Select(a => a.Value).SingleOrDefault();
        }

        #region serialization
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
            public Dictionary<string, HeightSpec.Container> Groups { get; set; }

            // ReSharper disable once MemberCanBePrivate.Local
            public ISelectorContainer[] Verticals { get; set; }

            // ReSharper disable once MemberCanBePrivate.Local
            public ISelectorContainer[] Floors { get; set; }

            public FloorSelector Unwrap()
            {
                return new FloorSelector(
                    Floors.Select(a => a.Unwrap()).ToArray(),
                    (Verticals ?? new ISelectorContainer[0]).Select(a => a.Unwrap()).ToArray(),
                    (Groups ?? new Dictionary<string, HeightSpec.Container>()).Select(a => new KeyValuePair<string, HeightSpec>(a.Key, a.Value.Unwrap())).ToArray()
                );
            }
        }
        #endregion
    }
}

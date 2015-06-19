using System;
using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec
{
    public class VerticalElementSpec
    {
        private readonly KeyValuePair<float, string[]>[] _tags;
        public IEnumerable<KeyValuePair<float, string[]>> Tags
        {
            get
            {
                return _tags;
            }
        }

        public FloorRef BottomFloor { get; private set; }
        public FloorRef TopFloor { get; private set; }

        public VerticalElementSpec(KeyValuePair<float, string[]>[] tags, FloorRef bottomFloor, FloorRef topFloor)
        {
            _tags = tags;
            TopFloor = topFloor;
            BottomFloor = bottomFloor;
        }

        internal class Container
        {
            public TagContainer Tags { get; set; }

            public FloorRef.Container Bottom { get; set; }
            public FloorRef.Container Top { get; set; }

            public VerticalElementSpec Unwrap()
            {
                return new VerticalElementSpec(
                    Tags.ToArray(),
                    Bottom.Unwrap(),
                    Top.Unwrap()
                );
            }
        }
    }
}

﻿
using System.Collections.Generic;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Ref
{
    public class NumRef
        : IRef
    {
        public int Number { get; private set; }

        public NumRef(int number)
        {
            Number = number;
        }

        public IEnumerable<FloorSelection> Match(int basements, FloorSelection[] floors)
        {
            //floors are top to bottom, so we need to convert the floor index
            int index = floors.Length - 1 - basements - Number;

            //If the index is out of range, match nothing
            if (index < 0 || index >= floors.Length)
                yield break;

            //Found a match!
            yield return floors[index];
        }

        internal class Container
            : IRefContainer
        {
            public int N { get; set; }

            private IRef _cached;

            public IRef Unwrap()
            {
                if (_cached == null)
                    _cached = new NumRef(N);
                return _cached;
            }
        }
    }
}
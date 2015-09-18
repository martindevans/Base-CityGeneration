using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using System.Collections.Generic;
using EpimetheusPlugins.Scripts;
using Myre.Collections;
using SwizzleMyVectors;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers
{
    public abstract class BaseMarker
        : BaseFloorSelector
    {
        public override float MinHeight
        {
            get { return 0; }
        }

        public override float MaxHeight
        {
            get { return 0; }
        }

        private readonly BaseFootprintAlgorithm[] _footprintAlgorithms;
        public IEnumerable<BaseFootprintAlgorithm> FootprintAlgorithms
        {
            get { return _footprintAlgorithms; }
        }

        protected BaseMarker(BaseFootprintAlgorithm[] footprintAlgorithms)
        {
            _footprintAlgorithms = footprintAlgorithms;
        }

        public IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint)
        {
            var wip = footprint;
            for (int i = 0; i < _footprintAlgorithms.Length; i++)
            {
                var alg = _footprintAlgorithms[i];
                wip = alg.Apply(random, metadata, wip, footprint);

                wip = Reduce(wip);
            }

            return wip;
        }

        /// <summary>
        /// Reduce the number of sides in this footprint
        /// </summary>
        /// <param name="footprint"></param>
        /// <returns></returns>
        private static IReadOnlyList<Vector2> Reduce(IReadOnlyList<Vector2> footprint)
        {
            if (footprint.Count <= 3)
                return footprint;

            List<Vector2> result = footprint.ToList();

            for (int i = 0; i < result.Count; i++)
            {
                if (footprint.Count <= 3)
                    break;

                //Get previous, current and next
                var a = result[(i + result.Count - 1) % result.Count];
                var b = result[i];
                var c = result[(i + 1) % result.Count];

                var ab = b - a;
                var bc = c - b;

                var area = Math.Abs(0.5f * ab.Cross(bc));

                //sin 2 degree * 0.5 * 2 * 2
                //i.e. we remove angle with less than the area of a 2 degree bend between 2 meter pieces
                if (area < 0.069798f)
                {
                    i--;
                    result.RemoveAt(i);
                }
            }

            return result;
        }

        public override IEnumerable<FloorRun> Select(Func<double> random, INamedDataCollection metadata, Func<string[], ScriptReference> finder)
        {
            return new FloorRun[1] {
                new FloorRun(new FloorSelection[0], this)
            };
        }

        internal abstract class BaseContainer
            : ISelectorContainer, IList<BaseFootprintAlgorithm.BaseContainer>
        {
            private readonly List<BaseFootprintAlgorithm.BaseContainer> _algorithms = new List<BaseFootprintAlgorithm.BaseContainer>(); 

            protected abstract BaseFloorSelector Unwrap();

            BaseFloorSelector ISelectorContainer.Unwrap()
            {
                return Unwrap();
            }

            #region ilist
            public IEnumerator<BaseFootprintAlgorithm.BaseContainer> GetEnumerator()
            {
                return _algorithms.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_algorithms).GetEnumerator();
            }

            public void Add(BaseFootprintAlgorithm.BaseContainer item)
            {
                _algorithms.Add(item);
            }

            public void Clear()
            {
                _algorithms.Clear();
            }

            public bool Contains(BaseFootprintAlgorithm.BaseContainer item)
            {
                return _algorithms.Contains(item);
            }

            public void CopyTo(BaseFootprintAlgorithm.BaseContainer[] array, int arrayIndex)
            {
                _algorithms.CopyTo(array, arrayIndex);
            }

            public bool Remove(BaseFootprintAlgorithm.BaseContainer item)
            {
                return _algorithms.Remove(item);
            }

            public int Count
            {
                get { return _algorithms.Count; }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }

            public int IndexOf(BaseFootprintAlgorithm.BaseContainer item)
            {
                return _algorithms.IndexOf(item);
            }

            public void Insert(int index, BaseFootprintAlgorithm.BaseContainer item)
            {
                _algorithms.Insert(index, item);
            }

            public void RemoveAt(int index)
            {
                _algorithms.RemoveAt(index);
            }

            public BaseFootprintAlgorithm.BaseContainer this[int index]
            {
                get { return _algorithms[index]; }
                set { _algorithms[index] = value; }
            }
            #endregion
        }
    }
}

using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using System.Collections.Generic;
using EpimetheusPlugins.Scripts;
using HandyCollections.Extensions;
using Myre.Collections;
using Poly2Tri.Utility;

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

        public IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> lot)
        {
            var wip = footprint;
            foreach (var alg in _footprintAlgorithms)
            {
                wip = alg.Apply(random, metadata, wip, footprint, lot);
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
            //Early exit, we can't do anything useful with a line!
            if (footprint.Count <= 3)
                return footprint;

            //Create a list with the points in
            var p = new Point2DList();
            p.AddRange(footprint.Append(footprint[0]).Select(a => new Point2D(a.X, a.Y)).ToArray());

            //If two consecutive points are in the same position, remove one
            p.RemoveDuplicateNeighborPoints();

            //Merge edges which are parallel (with a tolerance of 1 degree)
            //p.MergeParallelEdges(0.01745240643);
            p.Simplify();

            //Ensure shape is clockwise wound
            p.CalculateWindingOrder();
            if (p.WindingOrder != Point2DList.WindingOrderType.Clockwise)
            {
                if (p.WindingOrder != Point2DList.WindingOrderType.AntiClockwise)
                    throw new InvalidOperationException("Winding order is neither clockwise or anticlockwise");

                //We're done (but we need to correct the winding)
                return p.Select(a => new Vector2(a.Xf, a.Yf)).ToArray();
            }

            //We're done :D
            return p.Select(a => new Vector2(a.Xf, a.Yf)).ToArray();
        }

        public override IEnumerable<FloorRun> Select(Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder)
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

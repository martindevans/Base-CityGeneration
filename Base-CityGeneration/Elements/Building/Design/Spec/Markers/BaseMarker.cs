using System;
using System.Collections;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using System.Collections.Generic;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

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

        public override IEnumerable<FloorSelection> Select(Func<double> random, INamedDataCollection metadata, Func<string[], ScriptReference> finder)
        {
            yield break;
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

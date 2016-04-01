using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Placeholder.AI.Pathfinding.Graph.NavigationMesh;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public class Face<TV, TE, TF>
        : BaseTagged<TF, IFaceTag<TV, TE, TF>, Face<TV, TE, TF>>
    {
        #region fields
        /// <summary>
        /// One of the half edges bounding this face
        /// </summary>
        public HalfEdge<TV, TE, TF> Edge { get; internal set; }

        private readonly int _uid;
        internal int Id
        {
            get { return _uid; }
        }

        public IEnumerable<HalfEdge<TV, TE, TF>> Edges
        {
            get
            {
                Contract.Ensures(IsDeleted || Contract.Result<IEnumerable<HalfEdge<TV, TE, TF>>>() != null);
                Contract.Ensures(IsDeleted || Contract.ForAll(Contract.Result<IEnumerable<HalfEdge<TV, TE, TF>>>(), h => h != null));
                Contract.Ensures(Contract.Result<IEnumerable<HalfEdge<TV, TE, TF>>>() != null);

                var e = Edge;
                do
                {
                    yield return e;
                    e = e.Next;

                    if (e == null)
                        throw new InvalidMeshException("Found a null 'Next' pointer while walking around a face");

                } while (!ReferenceEquals(e, Edge));
            }
        }

        public IEnumerable<Face<TV, TE, TF>> Neighbours
        {
            get
            {
                Contract.Ensures(IsDeleted || Contract.Result<IEnumerable<Face<TV, TE, TF>>>() != null);
                Contract.Ensures(IsDeleted || Contract.ForAll(Contract.Result<IEnumerable<Face<TV, TE, TF>>>(), f => f != null));
                Contract.Ensures(Contract.Result<IEnumerable<Face<TV, TE, TF>>>() != null);

                return Edges
                    .Select(e => e.Pair.Face)
                    .Where(f => f != null)
                    .Distinct();
            }
        }

        public IEnumerable<Vertex<TV, TE, TF>> Vertices
        {
            get
            {
                Contract.Ensures(IsDeleted || Contract.Result<IEnumerable<Vertex<TV, TE, TF>>>() != null);
                Contract.Ensures(IsDeleted || Contract.ForAll(Contract.Result<IEnumerable<Vertex<TV, TE, TF>>>(), v => v != null));
                Contract.Ensures(Contract.Result<IEnumerable<Vertex<TV, TE, TF>>>() != null);

                return (from e in Edges
                        select e.EndVertex);
            }
        }

        public bool IsDeleted { get; internal set; }
        #endregion

        internal Face(int uid)
        {
            _uid = uid;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        internal class Comparer
            : IComparer<Face<TV, TE, TF>>
        {
            public int Compare(Face<TV, TE, TF> a, Face<TV, TE, TF> b)
            {
                return a.Id.CompareTo(b.Id);
            }
        }
    }
}

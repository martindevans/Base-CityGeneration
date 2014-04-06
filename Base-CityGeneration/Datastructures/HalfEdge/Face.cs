using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public class Face<TVertexTag, THalfEdgeTag, TFaceTag>
    {
        #region fields
        /// <summary>
        /// One of the half edges bounding this face
        /// </summary>
        public HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> Edge { get; internal set; }

        public TFaceTag Tag;

        public IEnumerable<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>> Edges
        {
            get
            {
                var e = Edge;
                do
                {
                    yield return e;
                    e = e.Next;
                } while (!ReferenceEquals(e, Edge));
            }
        }

        public IEnumerable<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>> Vertices
        {
            get
            {
                return (from e in Edges
                        select e.EndVertex);
            }
        }
        #endregion
    }
}

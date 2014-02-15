using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public class Face
    {
        #region fields
        /// <summary>
        /// One of the half edges bounding this face
        /// </summary>
        public HalfEdge Edge { get; internal set; }

        internal readonly Mesh Mesh;

        internal IFaceBuilder Builder;

        public IEnumerable<HalfEdge> Edges
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

        public IEnumerable<Vertex> Vertices
        {
            get
            {
                return (from e in Edges
                        select e.EndVertex);
            }
        }

        #endregion

        public Face(Mesh m)
        {
            Mesh = m;
        }

        public void Delete()
        {
            Mesh.Delete(this);
        }
    }
}

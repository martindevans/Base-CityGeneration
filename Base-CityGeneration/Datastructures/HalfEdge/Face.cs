using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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

        private readonly int _uid;
        private int Id
        {
            get { return _uid; }
        }

        private TFaceTag _tag;
        /// <summary>
        /// The tag attached to this face (may be null).
        /// If this tag implements IFaceTag then Attach and Detach will be called appropriately
        /// </summary>
        public TFaceTag Tag
        {
            get { return _tag; }
            set
            {
                var oldTag = _tag as IFaceTag<TVertexTag, THalfEdgeTag, TFaceTag>;
                if (oldTag != null)
                    oldTag.Detach(this);

                var newTag = value as IFaceTag<TVertexTag, THalfEdgeTag, TFaceTag>;
                if (newTag != null)
                    newTag.Attach(this);

                _tag = value;
            }
        }

        public IEnumerable<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>> Edges
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>>() != null);

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
                Contract.Ensures(Contract.Result<IEnumerable<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>>>() != null);

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

        internal class Comparer
            : IComparer<Face<TVertexTag, THalfEdgeTag, TFaceTag>>
        {
            public int Compare(Face<TVertexTag, THalfEdgeTag, TFaceTag> a, Face<TVertexTag, THalfEdgeTag, TFaceTag> b)
            {
                return a.Id.CompareTo(b.Id);
            }
        }
    }
}

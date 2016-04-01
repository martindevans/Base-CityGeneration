using System;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public interface IHalfEdgeTag<TV, TE, TF>
        : IAttachable<HalfEdge<TV, TE, TF>>
    {
        HalfEdge<TV, TE, TF> Edge { get; }
    }

    public abstract class BaseHalfEdgeTag<TV, TE, TF>
        : IHalfEdgeTag<TV, TE, TF>
    {
        public HalfEdge<TV, TE, TF> Edge { get; private set; }

        public void Attach(HalfEdge<TV, TE, TF> e)
        {
            if (Edge != null)
                throw new InvalidOperationException("Cannot attach: Tag is already attached to an edge");
            Edge = e;
        }

        public void Detach(HalfEdge<TV, TE, TF> e)
        {
            if (!Edge.Equals(e))
                throw new InvalidOperationException("Cannot detach: Tag is not connected to this edge");
            Edge = null;
        }
    }
}

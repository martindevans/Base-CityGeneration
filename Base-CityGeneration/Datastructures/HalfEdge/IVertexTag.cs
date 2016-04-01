using System;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public interface IVertexTag<TV, TE, TF>
        : IAttachable<Vertex<TV, TE, TF>>
    {
        Vertex<TV, TE, TF> Vertex { get; }
    }

    public abstract class BaseVertexTag<TV, TE, TF>
        : IVertexTag<TV, TE, TF>
    {
        public Vertex<TV, TE, TF> Vertex { get; private set; }

        public void Attach(Vertex<TV, TE, TF> f)
        {
            if (Vertex != null)
                throw new InvalidOperationException("Cannot attach: Tag is already attached to a vertex");
            Vertex = f;
        }

        public void Detach(Vertex<TV, TE, TF> f)
        {
            if (!Vertex.Equals(f))
                throw new InvalidOperationException("Cannot detach: Tag is not connected to this vertex");
            Vertex = null;
        }
    }
}

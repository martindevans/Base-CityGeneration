using System;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public interface IFaceTag<TV, TE, TF>
        : IAttachable<Face<TV, TE, TF>>
    {
        Face<TV, TE, TF> Face { get; }
    }

    public abstract class BaseFaceTag<TV, TE, TF>
        : IFaceTag<TV, TE, TF>
    {
        public Face<TV, TE, TF> Face { get; private set; }

        public virtual void Attach(Face<TV, TE, TF> f)
        {
            if (Face != null)
                throw new InvalidOperationException("Cannot attach: Tag is already attached to a vertex");
            Face = f;
        }

        public virtual void Detach(Face<TV, TE, TF> f)
        {
            if (!Face.Equals(f))
                throw new InvalidOperationException("Cannot detach: Tag is not connected to this vertex");
            Face = null;
        }
    }
}

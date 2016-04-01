namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public interface IAttachable<in T>
    {
        void Attach(T t);

        void Detach(T t); 
    }
}
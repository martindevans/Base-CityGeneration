using System.Collections.Generic;
using System.Linq;
using Placeholder.AI.Pathfinding.AStar;

namespace Base_CityGeneration.Datastructures.HalfEdge.Extensions
{
    public static class MeshExtensions
    {
        public static IEnumerable<HalfEdge> Pathfind(this Mesh m, Vertex start, Vertex end)
        {
            Pathfinder p = Pathfinder.Get();
            try
            {
                var edges = p.FindPath(start, end, (a, b) => (((Vertex) a).Position - ((Vertex) b).Position).Length());

                return edges.Cast<HalfEdge>();
            }
            finally
            {
                Pathfinder.Return(p);
            }
        }
    }
}

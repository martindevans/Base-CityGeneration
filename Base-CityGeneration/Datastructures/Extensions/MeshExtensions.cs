using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Datastructures.HalfEdge;
using Placeholder.AI.Pathfinding.AStar;

namespace Base_CityGeneration.Datastructures.Extensions
{
    public static class MeshExtensions
    {
// ReSharper disable UnusedParameter.Global
        public static IEnumerable<HalfEdge.HalfEdge> Pathfind(
            this Mesh m,
// ReSharper restore UnusedParameter.Global
            Vertex start,
            Vertex end
        )
        {
            if (start.Mesh != m)
                throw new ArgumentException("Start vertex is not contained in mesh for pathfind", "start");
            if (end.Mesh != m)
                throw new ArgumentException("End vertex is not contained in mesh for pathfind", "end");

            Pathfinder p = Pathfinder.Get();
            try
            {
                var edges = p.FindPath(start, end, (a, b) => (((Vertex) a).Position - ((Vertex) b).Position).Length());

                return edges.Cast<HalfEdge.HalfEdge>();
            }
            finally
            {
                Pathfinder.Return(p);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Elements.Generic;
using Base_CityGeneration.Elements.Roads;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Root
{
    /// <summary>
    /// A base class for cities, handles creating appropriate roads and junctions.
    /// 
    /// First Generates a mesh of edges (roads), vertices (junctions) and faces (city blocks).
    /// </summary>
    public abstract class BaseCityRoot
        :ProceduralRoot
    {
        private readonly float _laneWidth;
        private readonly float _sidewalkWidth;
        private readonly float _totalHeight;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="laneWidth">The wide of a road lane (see: https://en.wikipedia.org/wiki/Lane#Lane_width) </param>
        /// <param name="sidewalkWidth">The width of the sidewalk on a normal road</param>
        /// <param name="totalHeight">The total height of the city, half above ground and half below ground</param>
        protected BaseCityRoot(float laneWidth = 3.7f, float sidewalkWidth = 2, float totalHeight = 1000)
        {
            _laneWidth = laneWidth;
            _sidewalkWidth = sidewalkWidth;
            _totalHeight = totalHeight;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, Myre.Collections.INamedDataCollection hierarchicalParameters)
        {
            //Generate a topological map of city roads
            Mesh m = GenerateBlockMesh();

            //Materialize the topological map into a topographical one
            MaterializeMesh(m);
        }

        #region abstract stuff
        /// <summary>
        /// Generate a HalfEdge mesh which represents the layout of roads in the city
        /// </summary>
        /// <returns></returns>
        protected abstract Mesh GenerateBlockMesh();

        /// <summary>
        /// Choose the set of possible scripts to place in the given block
        /// </summary>
        /// <param name="topology">The topology of this block. Information such as what blocks/roads are around this block</param>
        /// <param name="topography">The topography of this block. It's exact shape and size</param>
        /// <returns></returns>
        protected abstract IEnumerable<ScriptReference> ChooseBlockScript(Face topology, Prism topography);

        /// <summary>
        /// Choose the set of possible scripts to place along the given road
        /// </summary>
        /// <param name="topology">The topology of this block. Information such as neighbouring blocks and junctions</param>
        /// <param name="topography">The topography of this block. It's exact shape and size</param>
        /// <returns></returns>
        protected abstract IEnumerable<ScriptReference> ChooseRoadScript(HalfEdge topology, Prism topography);

        /// <summary>
        /// Choose a set of possible scripts to place at the given junction
        /// </summary>
        /// <param name="topology">The topology of this block. Information such as what roads are around this junction</param>
        /// <param name="topography">The topography of this block. It's exact shape and size</param>
        /// <returns></returns>
        protected abstract IEnumerable<ScriptReference> ChooseJunctionScript(Vertex topology, Prism topography);

        /// <summary>
        /// Calculate how many lanes the given road should have
        /// </summary>
        /// <param name="road"></param>
        /// <returns></returns>
        protected abstract int RoadLanes(HalfEdge road);
        #endregion

        /// <summary>
        /// Create a vertex builder which builds the geometry for the given vertex
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        protected virtual IVertexBuilder CreateVertexBuilder(Vertex vertex)
        {
            return new VertexJunctionBuilder(vertex);
        }

        /// <summary>
        /// Create an edge builder which builds the geometry for the given edge
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="roadLanes"></param>
        /// <returns></returns>
        protected virtual IHalfEdgeBuilder CreateHalfEdgeBuilder(HalfEdge edge, int roadLanes)
        {
            return new HalfEdgeRoadBuilder(edge, _laneWidth, _sidewalkWidth, roadLanes);
        }

        /// <summary>
        /// Create a face builder which builds the geometry for the given face
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        protected virtual IFaceBuilder CreateFaceBuilder(Face face)
        {
            return new FaceBlockBuilder(face);
        }

        private void MaterializeMesh(Mesh mesh)
        {
            //Attach builders to each part of the topological mesh
            foreach (var vertex in mesh.Vertices)
                vertex.Builder = CreateVertexBuilder(vertex);
            foreach (var halfEdge in mesh.HalfEdges.Where(e => e.IsPrimaryEdge))
                halfEdge.Builder = CreateHalfEdgeBuilder(halfEdge, RoadLanes(halfEdge));
            foreach (var face in mesh.Faces)
                face.Builder = CreateFaceBuilder(face);

            //Create junctions (appropriate shape for different widths of road)
            foreach (var vertex in mesh.Vertices)
                CreateJunction(_totalHeight, vertex);

            //Create roads (with appropriate widths)
            foreach (var edge in mesh.HalfEdges.Where(e => e.IsPrimaryEdge))
                CreateRoad(_totalHeight, edge);

            //Create blocks (with appropriate shapes for different road widths)
            foreach (var face in mesh.Faces)
                CreateBlock(_totalHeight, face);
        }

        private void Create<TTopology, TFallback>(float height, TTopology topology, Vector2[] shape, Func<TTopology, Prism, IEnumerable<ScriptReference>> choose)
        {
            var topography = new Prism(height, shape);

            //Choose a script for this junction (fallback to BasicJunction)
            var scripts = choose(topology, topography);
            if (!scripts.Any())
                scripts = ScriptReference.Find<TFallback>();

            //Create the block
            var c = CreateChild(topography, Quaternion.Identity, new Vector3(0, 0, 0), scripts) as IGrounded;
            if (c != null)
                c.GroundHeight = height / 2;
        }

        private void CreateJunction(float height, Vertex topology)
        {
            Create<Vertex, BasicJunction>(height, topology, topology.Builder.Shape, ChooseJunctionScript);
        }

        private void CreateRoad(float height, HalfEdge topology)
        {
            Create<HalfEdge, BasicRoad>(height, topology, topology.Builder.Shape, ChooseRoadScript);
        }

        private void CreateBlock(float height, Face topology)
        {
            Create<Face, SolidPlaceholderBuilding>(height, topology, topology.Builder.Shape, ChooseBlockScript);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Elements.Generic;
using Base_CityGeneration.Elements.Roads;
using Base_CityGeneration.Styles;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using System.Numerics;
using Myre.Collections;

namespace Base_CityGeneration.Elements.City
{
    /// <summary>
    /// A base class for cities, handles creating appropriate roads and junctions.
    /// 
    /// First Generates a mesh of edges (roads), vertices (junctions) and faces (city blocks).
    /// </summary>
    [ContractClass(typeof(BaseCityContracts))]
    public abstract class BaseCity
        :ProceduralScript
    {
        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            //Generate a topological map of city roads
            var m = GenerateBlockMesh();

            //Materialize the topological map into a topographical one
            MaterializeMesh(m);
        }

        #region abstract stuff
        /// <summary>
        /// Generate a HalfEdge mesh which represents the layout of roads in the city
        /// </summary>
        /// <returns></returns>
        protected abstract Mesh<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> GenerateBlockMesh();

        /// <summary>
        /// Choose the set of possible scripts to place in the given block
        /// </summary>
        /// <param name="topology">The topology of this block. Information such as what blocks/roads are around this block</param>
        /// <param name="topography">The topography of this block. It's exact shape and size</param>
        /// <returns></returns>
        protected virtual IEnumerable<ScriptReference> ChooseBlockScript(Face<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> topology, Prism topography)
        {
            return ScriptReference.Find<SolidPlaceholderBlock>();
        }

        /// <summary>
        /// Choose the set of possible scripts to place along the given road
        /// </summary>
        /// <param name="topology">The topology of this road. Information such as neighbouring blocks and junctions</param>
        /// <param name="topography">The topography of this road. It's exact shape and size</param>
        /// <returns></returns>
        protected virtual IEnumerable<ScriptReference> ChooseRoadScript(HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> topology, Prism topography)
        {
            return ScriptReference.Find<BasicRoad>();
        }

        /// <summary>
        /// Choose a set of possible scripts to place at the given junction
        /// </summary>
        /// <param name="topology">The topology of this junction. Information such as what roads are around this junction</param>
        /// <param name="topography">The topography of this junction. It's exact shape and size</param>
        /// <returns></returns>
        protected virtual IEnumerable<ScriptReference> ChooseJunctionScript(Vertex<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> topology, Prism topography)
        {
            return ScriptReference.Find<BasicJunction>();
        }

        /// <summary>
        /// Calculate how many lanes the given road should have
        /// </summary>
        /// <param name="road"></param>
        /// <returns></returns>
        protected abstract uint RoadLanes(HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> road);
        #endregion

        /// <summary>
        /// Create a vertex builder which builds the geometry for the given vertex
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        protected virtual IVertexBuilder CreateVertexBuilder(Vertex<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> vertex)
        {
            Contract.Requires(vertex != null);
            Contract.Ensures(Contract.Result<IVertexBuilder>() != null);

            return new VertexJunctionBuilder(vertex);
        }

        /// <summary>
        /// Create an edge builder which builds the geometry for the given edge
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="roadLanes"></param>
        /// <returns></returns>
        protected virtual IHalfEdgeBuilder CreateHalfEdgeBuilder(HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> edge, uint roadLanes)
        {
            Contract.Requires(edge != null);
            Contract.Requires(HierarchicalParameters != null);
            Contract.Ensures(Contract.Result<IHalfEdgeBuilder>() != null);

            var road = HierarchicalParameters.RoadLaneWidth(Random);
            var path = HierarchicalParameters.RoadSidewalkWidth(Random);

            return new HalfEdgeRoadBuilder(edge, road, path, roadLanes);
        }

        /// <summary>
        /// Create a face builder which builds the geometry for the given face
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        protected virtual IFaceBuilder CreateFaceBuilder(Face<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> face)
        {
            Contract.Requires(face != null);
            Contract.Ensures(Contract.Result<IFaceBuilder>() != null);

            return new FaceBlockBuilder(face);
        }

        private void MaterializeMesh(Mesh<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> mesh)
        {
            Contract.Requires(mesh != null);

            //Generate default footpath data
            HierarchicalParameters.RoadSidewalkHeight(Random);
            HierarchicalParameters.RoadSidewalkWidth(Random);
            HierarchicalParameters.RoadLaneWidth(Random);
            HierarchicalParameters.RoadSidewalkMaterial(Random);

            //Generate default building data
            HierarchicalParameters.MaximumBuildingHeight(Random);

            //Attach builders to each part of the topological mesh
            foreach (var vertex in mesh.Vertices.Where(v => v.Tag == null))
                vertex.Tag = CreateVertexBuilder(vertex);
            foreach (var halfEdge in mesh.HalfEdges.Where(e => e.IsPrimaryEdge && e.Tag == null))
                halfEdge.Tag = CreateHalfEdgeBuilder(halfEdge, RoadLanes(halfEdge));
            foreach (var face in mesh.Faces.Where(f => f.Tag == null))
                face.Tag = CreateFaceBuilder(face);

            //Create junctions (appropriate shape for different widths of road)
            foreach (var vertex in mesh.Vertices)
                CreateJunction(Bounds.Height, vertex);

            //Create roads (with appropriate widths)
            foreach (var edge in mesh.HalfEdges.Where(e => e.IsPrimaryEdge))
                CreateRoad(Bounds.Height, edge);

            //Create blocks (with appropriate shapes for different road widths)
            foreach (var face in mesh.Faces)
                CreateBlock(Bounds.Height, face);
        }

        private IGrounded Create<TTopology>(float height, TTopology topology, IReadOnlyCollection<Vector2> shape, Func<TTopology, Prism, IEnumerable<ScriptReference>> choose)
        {
            Contract.Requires(topology != null);
            Contract.Requires(choose != null);

            if (shape == null || shape.Count < 3)
                return null;

            var topography = new Prism(height, shape);

            //Choose a script for this junction (fallback to BasicJunction)
            var scripts = choose(topology, topography);
            if (!scripts.Any())
                return null;

            //Create the block
            var c = CreateChild(topography, Quaternion.Identity, new Vector3(0, 0, 0), scripts, false) as IGrounded;
            if (c != null)
                c.GroundHeight = height / 2;

            return c;
        }

        private IGrounded CreateJunction(float height, Vertex<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> topology)
        {
            Contract.Requires(topology != null);

            var result = Create<Vertex<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder>>(height, topology, topology.Tag.Shape, ChooseJunctionScript);

            var junction = result as IJunction;
            if (junction != null)
                junction.Vertex = topology;

            return result;
        }

        private IGrounded CreateRoad(float height, HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> topology)
        {
            Contract.Requires(topology != null && topology.Tag != null);

            var result = Create<HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder>>(height, topology, topology.Tag.Shape, ChooseRoadScript);

            var road = result as IRoad;
            if (road != null)
                road.HalfEdge = topology;

            return result;
        }

        private IGrounded CreateBlock(float height, Face<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> topology)
        {
            Contract.Requires(topology != null && topology.Tag != null);

            var result = Create<Face<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder>>(height, topology, topology.Tag.Shape, ChooseBlockScript);

            //var block = result as IBlock;
            //if (block != null)
            //    block.Face = topology;

            return result;
        }
    }

    [ContractClassFor(typeof(BaseCity))]
    internal abstract class BaseCityContracts
        : BaseCity
    {
        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return default(bool);
        }

        protected override Mesh<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> GenerateBlockMesh()
        {
            Contract.Ensures(Contract.Result<Mesh<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder>>() != null);

            return default(Mesh<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder>);
        }

        protected override uint RoadLanes(HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> road)
        {
            Contract.Requires(road != null);

            return default(uint);
        }
    }
}

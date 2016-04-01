//using System;
//using System.Collections.Generic;
//using System.Diagnostics.Contracts;
//using System.Linq;
//using System.Numerics;
//using Base_CityGeneration.Datastructures.Extensions;
//using Base_CityGeneration.Datastructures.HalfEdge;
//using EpimetheusPlugins.Extensions;
//using SwizzleMyVectors;
//using SwizzleMyVectors.Geometry;

//namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan
//{
//    /// <summary>
//    /// Extensions relating to treating a half edge mesh as a floor plan
//    /// </summary>
//    public static class MeshCorridorExtensions
//    {
//        /// <summary>
//        /// Insert corridors into the mesh
//        /// </summary>
//        /// <param name="mesh"></param>
//        /// <param name="corridors"></param>
//        public static Mesh<IVertexTag, IHalfEdgeTag, IFaceTag> InsertCorridors(this Mesh<IVertexTag, IHalfEdgeTag, IFaceTag> mesh, IDictionary<HalfEdge<IVertexTag, IHalfEdgeTag, IFaceTag>, float> corridors)
//        {
//            Contract.Requires(mesh != null);
//            Contract.Requires(corridors != null);
//            Contract.Ensures(Contract.Result<Mesh<IVertexTag, IHalfEdgeTag, IFaceTag>>() != null);

//            //Deduplicate corridors - we could have primary and non-primary edges in here
//            //Find all non-primaries and fix them to be primaries
//            DeduplicateCorridorSpecifications(corridors);

//            //Replace existing tags (temporarily) with new tags which hold the data we need for building corridors
//            //These tags hold a reference to the old tag and *do not* call detach on them. We'll put the originals back in place later (without calling attach).
//            //The original tags cannot tell they were ever not the tag.
//            CreateBuilderTags(mesh, corridors);
//            try
//            {
//                //Calculate where the corridor will go (which side of the edge)
//                //middle along edge if there are rooms both sides
//                //Cut into room entirely if there is only one room
//                //Throw if no neighbouring rooms
//                foreach (var corridor in corridors)
//                    ((IHalfEdgeCorridorBuilder)corridor.Key.Tag).CalculateSides();

//                //Now we know where the corridors will go, calculate the shapes of the junctions between corridors
//                foreach (var vertex in mesh.Vertices)
//                    ((VertexCorridorBuilder)vertex.Tag).CalculateShape();

//                throw new NotImplementedException("Delete vertices which need replacing, create new junctions, corridors and rooms (in that order)");
//                throw new NotImplementedException("Restore tags from deleted room to their replacements");
//                throw new NotImplementedException("Add tags to junctions/corridors indicating what they are");

//                return mesh;
//            }
//            finally
//            {
//                //Unwrap all the tags which are not null and got wrapped
//                RemoveBuilderTags(mesh);
//            }
//        }

//        private static void CreateBuilderTags(Mesh<IVertexTag, IHalfEdgeTag, IFaceTag> mesh, IDictionary<HalfEdge<IVertexTag, IHalfEdgeTag, IFaceTag>, float> corridors)
//        {
//            Contract.Requires(mesh != null);
//            Contract.Requires(corridors != null);

//            mesh.WrapTags(v => new VertexCorridorBuilder(v.Tag), h =>
//            {
//                if (h.IsPrimaryEdge)
//                {
//                    //Associate tag with corridor width (if there is one for this edge)
//                    float? width = null;
//                    float w;
//                    if (corridors.TryGetValue(h, out w))
//                        width = w;
//                    return new HalfEdgeCorridorBuilder(h.Tag, width);
//                }
//                else
//                {
//                    //Create a tag which simply accesses the data from it's primary (appropriately reversed)
//                    return new HalfEdgeSwitcharoo(h.Tag);
//                }
//            }, f => new FaceCorridorBuilder(f.Tag), true);
//        }

//        private static void RemoveBuilderTags(Mesh<IVertexTag, IHalfEdgeTag, IFaceTag> mesh)
//        {
//            Contract.Requires(mesh != null);

//            mesh.WrapTags(v => {
//                var wrapper = v.Tag as VertexCorridorBuilder;
//                return wrapper != null ? wrapper.Wrapped : v.Tag;
//            }, h => {
//                var wrapper = h.Tag as IHalfEdgeCorridorBuilder;
//                return wrapper != null ? wrapper.Wrapped : h.Tag;
//            }, f => {
//                var wrapper = f.Tag as FaceCorridorBuilder;
//                return wrapper != null ? wrapper.Wrapped : f.Tag;
//            }, false);
//        }

//        /// <summary>
//        /// Modify the given dictionary to only contain primary edges. Throws if primary and secondary are specified for same edge with different widths
//        /// </summary>
//        /// <param name="corridors"></param>
//        private static void DeduplicateCorridorSpecifications(IDictionary<HalfEdge<IVertexTag, IHalfEdgeTag, IFaceTag>, float> corridors)
//        {
//            Contract.Requires(corridors != null);

//            var toRemove = new List<HalfEdge<IVertexTag, IHalfEdgeTag, IFaceTag>>();
//            var toAdd = new List<KeyValuePair<HalfEdge<IVertexTag, IHalfEdgeTag, IFaceTag>, float>>();
//            foreach (var corridor in corridors.Where(e => !e.Key.IsPrimaryEdge))
//            {
//                if (!corridors.ContainsKey(corridor.Key.Pair))
//                {
//                    //The pair isn't in the map, which makes this easy
//                    toAdd.Add(new KeyValuePair<HalfEdge<IVertexTag, IHalfEdgeTag, IFaceTag>, float>(corridor.Key.Pair, corridor.Value));
//                }
//                else
//                {
//                    //The pair is in the map! Check if it's the same width
//                    //If it's not the same width this can't be resolved, throw an exception :(
//                    var primaryWidth = corridors[corridor.Key.Pair];
//                    if (!primaryWidth.TolerantEquals(corridor.Value, 0.1f))
//                        throw new InvalidOperationException(string.Format("Corridor specified twice for {0} width 2 widths {1} and {2}", corridor.Key, primaryWidth, corridor.Value));
//                }

//                toRemove.Add(corridor.Key);
//            }

//            //Remove the duplicate edges
//            foreach (var halfEdge in toRemove)
//            {
//                if (!corridors.Remove(halfEdge))
//                    throw new InvalidOperationException("Marked an edge for removal, but it's not in the dictionary");
//            }

//            //Add in primary pairs
//            foreach (var halfEdge in toAdd)
//                corridors.Add(halfEdge);
//        }

//        private class VertexCorridorBuilder
//            : BaseVertexTag
//        {
//            public IVertexTag Wrapped { get; private set; }

//            public IEnumerable<IHalfEdgeCorridorBuilder> InwardCorridors
//            {
//                get { return Vertex.OrderedEdgeTags().Cast<IHalfEdgeCorridorBuilder>().Where(e => e.IsCorridor); }
//            }

//            private IReadOnlyList<Vector2> _shape; 
//            public IReadOnlyList<Vector2> Shape
//            {
//                get { return _shape; }
//            }

//            public VertexCorridorBuilder(IVertexTag wrapped)
//            {
//                Wrapped = wrapped;
//            }

//            /// <summary>
//            /// Calculate the shape of the junction at this vertex
//            /// </summary>
//            public void CalculateShape()
//            {
//                //Get corridors which end at this vertex, ordered by angle (starting at an arbitrary one)
//                var inward = InwardCorridors.ToArray();

//                switch (inward.Length)
//                {
//                    case 1:
//                        throw new NotImplementedException();

//                        //Push this vertex apart into 2 vertices

//                        //return GenerateDeadEnd(_vertex.Edges.Single());
//                    default:
//                        throw new NotImplementedException();
//                        //return GenerateNWayJunction();
//                        break;
//                }

//                _shape = null;
//            }
//        }

//        [ContractClass(typeof(IHalfEdgeCorridorBuilderContracts))]
//        private interface IHalfEdgeCorridorBuilder : IHalfEdgeTag
//        {
//            IHalfEdgeTag Wrapped { get; }

//            float? Width { get; }
//            Ray2 LeftSide { get; }
//            Ray2 RightSide { get; }

//            bool IsCorridor { get; }

//            void CalculateSides();
//        }

//        [ContractClassFor(typeof(IHalfEdgeCorridorBuilder))]
//        private abstract class IHalfEdgeCorridorBuilderContracts
//            : IHalfEdgeCorridorBuilder
//        {
//            public void Attach(HalfEdge<IVertexTag, IHalfEdgeTag, IFaceTag> t)
//            {
//                throw new NotImplementedException();
//            }

//            public void Detach(HalfEdge<IVertexTag, IHalfEdgeTag, IFaceTag> t)
//            {
//                throw new NotImplementedException();
//            }

//            public HalfEdge<IVertexTag, IHalfEdgeTag, IFaceTag> Edge
//            {
//                get { throw new NotImplementedException(); }
//            }

//            public IHalfEdgeTag Wrapped
//            {
//                get { throw new NotImplementedException(); }
//            }

//            public float? Width
//            {
//                get { throw new NotImplementedException(); }
//            }

//            public Ray2 LeftSide
//            {
//                get
//                {
//                    Contract.Requires(IsCorridor);
//                    return default(Ray2);
//                }
//            }

//            public Ray2 RightSide
//            {
//                get
//                {
//                    Contract.Requires(IsCorridor);
//                    return default(Ray2);
//                }
//            }

//            public bool IsCorridor
//            {
//                get { throw new NotImplementedException(); }
//            }

//            public void CalculateSides()
//            {
//                throw new NotImplementedException();
//            }
//        }

//        private class HalfEdgeCorridorBuilder
//            : BaseHalfEdgeTag, IHalfEdgeCorridorBuilder
//        {
//            public IHalfEdgeTag Wrapped { get; private set; }

//            /// <summary>
//            /// The width of the corridor which should be placed along this edge (null if there is no corridor)
//            /// </summary>
//            public float? Width { get; private set; }

//            /// <summary>
//            /// Indicates if this edge is a corridor
//            /// </summary>
//            public bool IsCorridor { get { return Width.HasValue; } }

//            private Ray2? _leftSide;
//            public Ray2 LeftSide
//            {
//                get
//                {
//                    Contract.Assume(_leftSide.HasValue);
//                    return _leftSide.Value;
//                }
//            }

//            private Ray2? _rightSide;
//            public Ray2 RightSide
//            {
//                get
//                {
//                    Contract.Assume(_rightSide.HasValue);

//                    return _rightSide.Value;
//                }
//            }

//            public HalfEdgeCorridorBuilder(IHalfEdgeTag wrapped, float? width)
//            {
//                Wrapped = wrapped;
//                Width = width;
//            }

//            public void CalculateSides()
//            {
//                Contract.Assume(Width.HasValue);

//                //Sanity check
//                if (Edge.Face == null && Edge.Pair.Face == null)
//                    throw new InvalidOperationException("Cannot create a corridor along an edge with no adjacent rooms");

//                //Determine if this is an external edge
//                if (Edge.Face == null || Edge.Pair.Face == null)
//                {
//                    //External wall (face on one side is null)
//                    if (Edge.Face == null)
//                    {
//                        //Push Left
//                        _rightSide = new Ray2(Edge.StartVertex.Position, Edge.Segment.Line.Direction);
//                        _leftSide = new Ray2(Edge.StartVertex.Position - Edge.Segment.Line.Direction.Perpendicular() * Width.Value, Edge.Segment.Line.Direction);
//                    }
//                    else
//                    {
//                        //Push right
//                        _rightSide = new Ray2(Edge.StartVertex.Position + Edge.Segment.Line.Direction.Perpendicular() * Width.Value, Edge.Segment.Line.Direction);
//                        _leftSide = new Ray2(Edge.StartVertex.Position, Edge.Segment.Line.Direction);
//                    }
//                }
//                else
//                {
//                    //Internal wall (faces on both sides)
//                    _rightSide = new Ray2(Edge.StartVertex.Position + Edge.Segment.Line.Direction.Perpendicular() * Width.Value / 2, Edge.Segment.Line.Direction);
//                    _leftSide = new Ray2(Edge.StartVertex.Position - Edge.Segment.Line.Direction.Perpendicular() * Width.Value / 2, Edge.Segment.Line.Direction);
//                }
//            }
//        }

//        private class HalfEdgeSwitcharoo
//            : BaseHalfEdgeTag, IHalfEdgeCorridorBuilder
//        {
//            public IHalfEdgeTag Wrapped { get; private set; }

//            private HalfEdgeCorridorBuilder _pairTag
//            {
//                get { return (HalfEdgeCorridorBuilder)Edge.Pair.Tag; }
//            }

//            public float? Width
//            {
//                get { return _pairTag.Width; }
//            }

//            public bool IsCorridor
//            {
//                get { return _pairTag.IsCorridor; }
//            }

//            public Ray2 LeftSide
//            {
//                get { return _pairTag.RightSide; }
//            }

//            public Ray2 RightSide
//            {
//                get { return _pairTag.LeftSide; }
//            }

//            public HalfEdgeSwitcharoo(IHalfEdgeTag wrapped)
//            {
//                Wrapped = wrapped;
//            }

//            public void CalculateSides()
//            {
//                _pairTag.CalculateSides();
//            }
//        }

//        private class FaceCorridorBuilder
//            : BaseFaceTag
//        {
//            public IFaceTag Wrapped { get; private set; }

//            public FaceCorridorBuilder(IFaceTag wrapped)
//            {
//                Wrapped = wrapped;
//            }
//        }
//    }
//}

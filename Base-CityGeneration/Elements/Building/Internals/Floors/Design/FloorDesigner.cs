using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Utilities.Numbers;
using CGAL_StraightSkeleton_Dotnet;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using JetBrains.Annotations;
using Myre.Collections;
using SharpYaml.Serialization;
using SwizzleMyVectors.Geometry;

using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    public class FloorDesigner
    {
        #region field and properties
        public IEnumerable<KeyValuePair<string, string>> Tags { get; private set; }
        public Guid Id { get; private set; }
        public string Description { get; private set; }

        private readonly IReadOnlyCollection<ISpaceSpecProducer> _rooms;
        public IReadOnlyCollection<ISpaceSpecProducer> Rooms
        {
            get { return _rooms; }
        }
        #endregion

        private FloorDesigner(Dictionary<string, string> tags, Guid id, string description, IReadOnlyCollection<ISpaceSpecProducer> rooms)
        {
            Tags = tags;
            Id = id;
            Description = description;

            _rooms = rooms;
        }

        public FloorPlan Design(Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, IReadOnlyList<Vector2> footprint)
        {
            //Generate set of required spaces
            var requiredSpecs = _rooms.SelectMany(r => r.Produce(true, random, metadata));

            FloorPlan result = new FloorPlan(footprint);

            
            //Generate a floor skeleton to lay hallways along and subdivide the floor into regions
            var regions = GenerateRegions(footprint);

            //Connect external doors to hallway
            //Connect vertical features to hallway
            //  - Either create them on the corridor
            //  - Or create a new corridor to the vertical

            //Split space into regions (bounded by hallways)

            //Place rooms and shuffle to maximise satisfied constraints (this may be a little complex!)

            //If a space is passthrough merge it into adjacent hallways and expand it to fill space

            return result;
        }

        private static IEnumerable<Edge> GenerateRegions(IReadOnlyList<Vector2> footprint)
        {
            using (var straightSkeleton = StraightSkeleton.Generate(footprint))
            {
                //Slice the floorplan up by the lines of the straight skeleton
                List<IReadOnlyList<Vector2>> parts = new List<IReadOnlyList<Vector2>> { footprint };
                foreach (var edge in straightSkeleton.Skeleton)
                {
                    var sliceLine = new Line2D(edge.Start.Position, edge.End.Position - edge.Start.Position);
                    parts = (from part in parts
                             from resultPart in part.SlicePolygon(sliceLine)
                             select (IReadOnlyList<Vector2>)resultPart.ToList()).ToList();
                }

                DrawOutlines(parts);

            }

            return new Edge[0];
        }

        #region serialization
        private static Serializer CreateSerializer()
        {
            var serializer = new Serializer(new SerializerSettings {
                EmitTags = true,
            });

            //Root type
            serializer.Settings.RegisterTagMapping("Floorplan", typeof(Container));

            //space area types
            serializer.Settings.RegisterTagMapping("Room", typeof(RoomSpec.Container));
            serializer.Settings.RegisterTagMapping("Group", typeof(GroupSpec.Container));
            serializer.Settings.RegisterTagMapping("Repeat", typeof(RepeatSpec.Container));

            //Constraints
            serializer.Settings.RegisterTagMapping("Exterior", typeof(Exterior.Container));
            serializer.Settings.RegisterTagMapping("Area", typeof(Area.Container));

            //Connections
            serializer.Settings.RegisterTagMapping("Not", typeof(Invert.Container));
            serializer.Settings.RegisterTagMapping("IdRef", typeof(IdRef.Container));
            serializer.Settings.RegisterTagMapping("Either", typeof(Either.Container));
            serializer.Settings.RegisterTagMapping("Tagged", typeof(TaggedRef.Container));
            serializer.Settings.RegisterTagMapping("RegexIdRef", typeof(RegexIdRef.Container));

            //Utility types
            serializer.Settings.RegisterTagMapping("NormalValue", typeof(NormallyDistributedValue.Container));
            serializer.Settings.RegisterTagMapping("UniformValue", typeof(UniformlyDistributedValue.Container));

            return serializer;
        }

        public static FloorDesigner Deserialize(TextReader reader)
        {
            return CreateSerializer().Deserialize<Container>(reader).Unwrap();
        }

        internal class Container
        {
            //Collection of unused objects, helpful for writing scripts
            public List<object> Aliases { get; [UsedImplicitly] set; }

            // ReSharper disable once CollectionNeverUpdated.Global
            public Dictionary<string, string> Tags { get; [UsedImplicitly] set; }
            public string Id { get; [UsedImplicitly] set; }
            public string Description { get; [UsedImplicitly] set; }

            public ISpaceSpecProducerContainer[] Rooms { get; [UsedImplicitly] set; }

            public FloorDesigner Unwrap()
            {
                return new FloorDesigner(Tags, Guid.Parse(Id ?? Guid.NewGuid().ToString()), Description, Rooms.Select(a => a.Unwrap()).ToArray());
            }
        }
        #endregion

        #region Visualisation
        private static void DrawOutlines(IReadOnlyList<IReadOnlyList<Vector2>> parts)
        {
            var svg = new StringBuilder();
            svg.Append("<svg width=\"1000\" height=\"1000\"><g transform=\"translate(210, 210)\">");
            foreach (var part in parts)
                svg.Append(ToSvgPath(part, 10, "blue"));
            Console.WriteLine(svg);  
        }

        private static void DrawSkeleton(IEnumerable<Edge> borders, IEnumerable<Edge> spokes, IEnumerable<Edge> skeleton)
        {
            var svg = new StringBuilder();
            svg.Append("<svg width=\"1000\" height=\"1000\"><g transform=\"translate(210, 210)\">");
            svg.Append(string.Join("", ToSvgPaths(borders, 10, "blue")));
            svg.Append(string.Join("", ToSvgPaths(spokes, 10, "red")));
            svg.Append(string.Join("", ToSvgPaths(skeleton, 10, "green")));
            Console.WriteLine(svg);
        }

        private static void DrawSkeleton(IEnumerable<Edge> edges)
        {
            var svg = new StringBuilder();
            svg.Append("<svg width=\"1000\" height=\"1000\"><g transform=\"translate(210, 210)\">");
            svg.Append(string.Join("", ToSvgPaths(edges.Where(e => e.Type == EdgeType.Border), 10, "blue")));
            svg.Append(string.Join("", ToSvgPaths(edges.Where(e => e.Type == EdgeType.Spoke), 10, "red")));
            svg.Append(string.Join("", ToSvgPaths(edges.Where(e => e.Type == EdgeType.Skeleton), 10, "green")));
            Console.WriteLine(svg);
        }

        private static IEnumerable<string> ToSvgPaths(IEnumerable<Edge> edges, float scale, string color)
        {
            foreach (var edge in edges)
                yield return string.Format("<path fill=\"none\" stroke=\"" + color + "\" d=\"M{0} {1} L{2} {3}\"></path>", edge.Start.Position.X * scale, edge.Start.Position.Y * scale, edge.End.Position.X * scale, edge.End.Position.Y * scale);
        }

        private static string ToSvgPath(IReadOnlyList<Vector2> shape, float scale, string color)
        {
            var builder = new StringBuilder("<path fill=\"none\" stroke=\"" + color + "\" d=\"");

            builder.Append(string.Format("M {0} {1} ", shape[0].X * scale, shape[0].Y * scale));
            for (var i = 1; i < shape.Count; i++)
                builder.Append(string.Format("L {0} {1} ", shape[i].X * scale, shape[i].Y * scale));

            builder.Append("Z\"></path>");

            return builder.ToString();
        }
        #endregion
    }
}

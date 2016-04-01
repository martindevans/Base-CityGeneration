using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Datastructures.HalfEdge;
using EpimetheusPlugins.Procedural.Utilities;

namespace Base_CityGeneration.Datastructures.Extensions
{
    public static class MeshWallExtensions
    {
        /// <summary>
        /// Shrink faces by wall thickness distance and create wall faces in the new space
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="wallThickness"></param>
        /// <param name="wallSectionTag"></param>
        public static Mesh<TV, TE, TF> CreateRoomWalls<TV, TE, TF>(this Mesh<TV, TE, TF> mesh, Func<TF, float> wallThickness, Func<Walls.Section, TF> wallSectionTag)
        {
            Contract.Requires(mesh != null);
            Contract.Requires(wallThickness != null);
            Contract.Ensures(Contract.Result<Mesh<TV, TE, TF>>() != null);

            //We're going to be changing the faces, so we need to copy the set before we change it and break the enumerator!
            var faces = mesh.Faces.ToArray();

            var vertices = new List<Vertex<TV, TE, TF>>(10);

            foreach (var face in faces)
            {
                //Read out the thickness of the walls for this room (do this *before* detaching the tag, as that seems like something which could break whatever this Func does)
                var thickness = wallThickness(face.Tag);
                Contract.Assume(thickness > 0);

                //Detach the tag from the old face so that we can attach it to the replacement later
                var tag = face.Tag;
                face.Tag = default(TF);

                //Save the shape of the face and delete it
                vertices.Clear();
                vertices.AddRange(face.Vertices);
                mesh.Delete(face);

                //We want to be able to distinguish between facades (straight walls along the edge of a room) and corner sections
                //Sections() will work out the inner shape (shrunk inwards) and then calculate the appropriate section parts
                Vector2[] inner;
                var sections = vertices.Select(v => v.Position).Sections(thickness, out inner);

                //Create faces for all the parts of the wall
                foreach (var section in sections)
                {
                    var sFace = mesh.GetOrConstructFace(
                        mesh.GetOrConstructVertex(section.A),
                        mesh.GetOrConstructVertex(section.B),
                        mesh.GetOrConstructVertex(section.C),
                        mesh.GetOrConstructVertex(section.D)
                    );

                    sFace.Tag = wallSectionTag(section);
                }

                //Now create a face for the room itself in the open space in the middle
                var room = mesh.GetOrConstructFace(
                    inner.Select(mesh.GetVertex).ToArray()
                );
                room.Tag = tag;
            }

            return mesh;
        }
    }
}

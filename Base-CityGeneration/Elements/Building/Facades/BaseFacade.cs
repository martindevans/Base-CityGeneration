using System;
using System.Collections.Generic;
using Base_CityGeneration.Styles;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using EpimetheusPlugins.Services.CSG;
using System.Numerics;
using EpimetheusPlugins.Entities.Prefabs.Graphical;
using Myre;
using Myre.Collections;
using Myre.Extensions;
using SwizzleMyVectors;

using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Base_CityGeneration.Elements.Building.Facades
{
    /// <summary>
    /// A facade is a section of wall divided into sections along it's (horizontal) length
    /// </summary>
    [Script("3AB052C6-D54C-4AE0-905E-9C99FAC9C0F7", "Base Facade")]
    public class BaseFacade
        : ProceduralScript, IFacade
    {
        public Walls.Section Section { get; set; }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return bounds.Footprint.Count == 4;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            //Create a brush which will represent our facade
            var facade = CreateFacade(bounds, geometry, hierarchicalParameters);

            //Convert stamps and apply them to the facade brush
            var stamps = EmbossingStamps(hierarchicalParameters, Section.Width, bounds.Height);
            foreach (var stamp in stamps)
            {
                var brush = ConvertStampToBrush(Section, stamp, stamp.Material, geometry, hierarchicalParameters);
                if (brush == null)
                    continue;

                //Add the shape if this is additive. However if this is a glass stamp ignore the additive flag and subtract always.
                facade = (stamp.Additive && !stamp.GlassFill.HasValue) ? facade.Union(brush) : facade.Subtract(brush);

                //Create a block of glass in the appropriate place
                if (stamp.GlassFill.HasValue)
                {
                    //Create a brusg for the glass. Either reuse the existing brush (if the material is the same) or create a new one with the glass material
                    var glassBrush = stamp.GlassFill.Value.Material == stamp.Material ? brush : ConvertStampToBrush(Section, stamp, stamp.GlassFill.Value.Material, geometry, hierarchicalParameters);

                    //Transform this with world-transform to place window in correct place in world
                    Window.Create(this, glassBrush.Transform(WorldTransformation), stamp.GlassFill.Value.Opacity, stamp.GlassFill.Value.Scattering, stamp.GlassFill.Value.Attenuation);
                }
            }

            //Place the geometry in the world
            //Clip:false here allows the facade to overhang the bounds it is contained within (the wall section). Facades almost always do this in practice...
            //...e.g. fancy window decorations stick out from the wall a little bit
            geometry.Union(facade, clip: false);
        }

        /// <summary>
        /// Given a wall section and a stamp, convert it into a brush which will apply the stamp to the wall
        /// </summary>
        /// <param name="section"></param>
        /// <param name="stamp"></param>
        /// <param name="material"></param>
        /// <param name="geometry"></param>
        /// <param name="hierarchicalParameters"></param>
        /// <returns></returns>
        private static ICsgShape ConvertStampToBrush(Walls.Section section, Stamp stamp, string material, ICsgFactory geometry, INamedDataProvider hierarchicalParameters)
        {
            material = material ?? hierarchicalParameters.GetValue(new TypedName<string>("material"));

            //Establish basis vectors
            var vOut = section.Normal;
            var vLeft = vOut.Perpendicular();

            //Calculate absolute thickness of stamp
            var thickness = (stamp.EndDepth - stamp.StartDepth) * section.Thickness;

            //Zero thickness stamp does nothing, early exit with null
            if (Math.Abs(thickness - 0) < float.Epsilon)
                return null;

            //new Matrix4x4 { Forward = new Vector3(vOut.X, 0, vOut.Y), Left = new Vector3(vLeft.X, 0, vLeft.Y), Up = Vector3.Up, M44 = 1 };
            var basis = Matrix4x4.Identity;
            basis = basis.Forward(new Vector3(vOut.X, 0, vOut.Y));
            basis = basis.Left(new Vector3(vLeft.X, 0, vLeft.Y));
            basis = basis.Up(Vector3.UnitY);
            basis.M44 = 1;

            //Calculate transform from prism lying flat in plane to vertically aligned in plane of facade
            var transform = Matrix4x4.CreateTranslation(0, -(section.Thickness - thickness) / 2 + stamp.StartDepth * section.Thickness, 0) *
                            Matrix4x4.CreateRotationX(-MathHelper.PiOver2) *
                            basis;

            //create brush
            return geometry.CreatePrism(material, stamp.Shape, Math.Abs(thickness)).Transform(transform);
        }

        /// <summary>
        /// Create the base block for this facade (which embossing stamps will be applied to)
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="geometry"></param>
        /// <param name="hierarchicalParameters"></param>
        /// <returns></returns>
        protected virtual ICsgShape CreateFacade(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            return geometry.CreatePrism(hierarchicalParameters.DefaultMaterial(Random), bounds.Footprint, bounds.Height);
        }

        /// <summary>
        /// Get the set of stamps to be applied to the surface of this facade
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<Stamp> EmbossingStamps(INamedDataCollection hierarchicalParameters, float width, float height)
        {
            yield break;
        }

        /// <summary>
        /// A shape pressed into or popped out of a surface
        /// </summary>
        public struct Stamp
        {
            /// <summary>
            /// The shape of this stamp in the plane. 0,0 indicates the center, negative values are to the left and down, positive values are to the right and up
            /// </summary>
            public readonly Vector2[] Shape;

            /// <summary>
            /// The depth into the facade to start the emboss. 0 indicate the inside, 1 indicates the outside
            /// </summary>
            public readonly float StartDepth;

            /// <summary>
            /// The depth into the facade to end the emboss. 0 indicate the inside, 1 indicates the outside
            /// </summary>
            public readonly float EndDepth;

            /// <summary>
            /// If true, this emboss will add material, if false this emboss will cut material away
            /// </summary>
            public readonly bool Additive;

            /// <summary>
            /// If not null, this space will be filled in with glass
            /// </summary>
            public readonly GlassInfo? GlassFill;

            /// <summary>
            /// The material of this stamp
            /// </summary>
            public readonly string Material;

            /// <summary>
            /// Create a new embossing stamp
            /// </summary>
            /// <param name="startDepth">The depth into the facade to start the emboss. 0 indicate the inside, 1 indicates the outside</param>
            /// <param name="endDepth">The depth into the facade to end the emboss. 0 indicate the inside, 1 indicates the outside</param>
            /// <param name="additive">If true, this emboss will add material, if false this emboss will cut material away</param>
            /// <param name="material">The material of this stamp</param>
            /// <param name="glassInfo">This stamp will be filled in with glass</param>
            /// <param name="shape">The shape of this stamp in the plane. 0,0 indicates the center, negative values are to the left and down, positive values are to the right and up</param>
            public Stamp(float startDepth, float endDepth, bool additive, string material, GlassInfo glassInfo, params Vector2[] shape)
            {
                StartDepth = startDepth;
                EndDepth = endDepth;
                Additive = additive;
                Material = material;
                Shape = shape;
                GlassFill = glassInfo;
            }

            /// <summary>
            /// Create a new embossing stamp
            /// </summary>
            /// <param name="startDepth">The depth into the facade to start the emboss. 0 indicate the inside, 1 indicates the outside</param>
            /// <param name="endDepth">The depth into the facade to end the emboss. 0 indicate the inside, 1 indicates the outside</param>
            /// <param name="additive">If true, this emboss will add material, if false this emboss will cut material away</param>
            /// <param name="material">The material of this stamp</param>
            /// <param name="shape">The shape of this stamp in the plane. 0,0 indicates the center, negative values are to the left and down, positive values are to the right and up</param>
            public Stamp(float startDepth, float endDepth, bool additive, string material, params Vector2[] shape)
            {
                StartDepth = startDepth;
                EndDepth = endDepth;
                Additive = additive;
                Material = material;
                Shape = shape;

                GlassFill = null;
            }
        }

        /// <summary>
        /// Information used to create a block of glass
        /// </summary>
        public struct GlassInfo
        {
            public readonly float Opacity;
            public readonly float Scattering;
            public readonly float Attenuation;
            public readonly string Material;

            public GlassInfo(float opacity, float scattering = 0.2f, float attenuation = 0.25f, string material = "glass")
            {
                Opacity = opacity;
                Scattering = scattering;
                Attenuation = attenuation;
                Material = material;
            }

            public static readonly GlassInfo ClearGlass = new GlassInfo(0.2f, 0.25f, 0.25f, "glass");
            public static readonly GlassInfo FoggyGlass = new GlassInfo(0.4f, 0.75f, 0.75f, "glass");
            public static readonly GlassInfo DarkGlass = new GlassInfo(0.6f, 1.0f, 1.0f, "glass");
        }
    }
}

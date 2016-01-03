using EpimetheusPlugins.Procedural;
using System.Numerics;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;

namespace Base_CityGeneration.Elements.Generic
{
    /// <summary>
    /// A element which is attached to the ground
    /// </summary>
    public interface IGrounded
        : ISubdivisionContext
    {
        /// <summary>
        /// THe height of the ground (in the local space of this node)
        /// </summary>
        float GroundHeight { get; set; }
    }

    public static class IGroundedExtensions
    {
        /// <summary>
        /// Calculate the Y offset to create something at to put it on the ground
        /// </summary>
        /// <param name="grounded">The node which is grounded</param>
        /// <param name="itemHeight">The height of the item which you want the *base* of at the ground height</param>
        /// <returns></returns>
        public static float GroundOffset(this IGrounded grounded, float itemHeight)
        {
            Contract.Requires(grounded != null);

            return grounded.GroundHeight - grounded.Bounds.Height / 2f + itemHeight / 2f;
        }

        public static void CreateFlatPlane(this IGrounded grounded, ISubdivisionGeometry geometry, string material, ReadOnlyCollection<Vector2> footprint, float height, float yOffset = 0)
        {
            Contract.Requires(grounded != null);
            Contract.Requires(geometry != null);
            Contract.Requires(footprint != null && footprint.Count >= 3);

            var offset = GroundOffset(grounded, height) + yOffset;

            var prism = geometry.CreatePrism(material, footprint, height).Transform(Matrix4x4.CreateTranslation(0, offset, 0));
            geometry.Union(prism);
        }
    }
}

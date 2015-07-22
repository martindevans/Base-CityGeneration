﻿using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public interface IHalfEdgeBuilder
    {
        HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> HalfEdge { get; }

        Vector2[] Shape { get; }

        Line2D Left { get; }
        Line2D Right { get; }

        Vector2 Direction { get; }

        /// <summary>
        /// Get the total width of this road (with lane count, lane width, sidewalk width all taken into account)
        /// </summary>
        float Width { get; }

        /// <summary>
        /// Set by the junction builder at the end of this edge, this is the left hand point at the start of the road
        /// </summary>
        Vector2 LeftStart { set; get; }

        /// <summary>
        /// Set by the junction builder at the end of this edge, this is the right hand point at the start of the road
        /// </summary>
        Vector2 RightStart { set; get; }

        /// <summary>
        /// Set by the junction builder at the end of this edge, this is the left hand point at the end of the road
        /// </summary>
        Vector2 LeftEnd { set; get; }

        /// <summary>
        /// Set by the junction builder at the end of this edge, this is the right hand point at the end of the road
        /// </summary>
        Vector2 RightEnd { set; get; }
    }
}

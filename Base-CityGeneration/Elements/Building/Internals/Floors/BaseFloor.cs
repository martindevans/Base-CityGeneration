using System;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using Base_CityGeneration.Elements.Generic;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Microsoft.Xna.Framework;
using Myre;
using Myre.Collections;
using Myre.Extensions;

namespace Base_CityGeneration.Elements.Building.Internals.Floors
{
    /// <summary>
    /// A floor placed into a section of empty space.
    /// </summary>
    public abstract class BaseFloor
        :ProceduralScript, IFloor
    {
        private readonly float _minHeight;
        private readonly float _maxHeight;
        private readonly float _floorThickness;
        private readonly float _ceilingThickness;

        public FloorPlan Plan { get; private set; }

        /// <summary>
        /// The index of this floor
        /// </summary>
        public int FloorIndex { get; set; }

        public IVerticalFeature[] Overlaps { get; set; }

        protected BaseFloor(float minHeight = 1.5f, float maxHeight = 4, float floorThickness = 0.1f, float ceilingThickness = 0.1f)
        {
            _minHeight = minHeight;
            _maxHeight = maxHeight;
            _floorThickness = floorThickness;
            _ceilingThickness = ceilingThickness;
        }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return bounds.Height >= _minHeight
                && bounds.Height <= _maxHeight;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            //Brush to fill the entire floor
            ICsgShape brush = geometry.CreatePrism(hierarchicalParameters.GetValue(new TypedName<string>("material")), bounds.Footprint, bounds.Height);

            ////Create rooms for all the vertical overlaps
            //foreach (var overlap in Overlaps)
            //{
            //    var points = overlap.Bounds.Footprint.ToArray();
            //    var w = overlap.WorldTransformation * InverseWorldTransformation;
            //    Vector2.Transform(points, ref w, points);
            //    var r = Plan.AddRoom(points, 0.1f, ScriptReference.Find<IdentityScript>(), false);
            //    if (r.Count() != 1)
            //        throw new InvalidOperationException("Adding Vertical element failed");
            //    r.Single().Tag = overlap;

            //    brush = brush.Subtract(geometry.CreatePrism(overlap.HierarchicalParameters.GetValue(new TypedName<string>("material")), points, bounds.Height));
            //}

            //Create rooms
            Plan = new FloorPlan(bounds.Footprint, hierarchicalParameters.GetMaybeValue(new TypedName<float>("external_wall_thickness")) ?? 0.25f);
            CreateRooms(Plan);
            Plan.Freeze();

            //Cut rooms out of brush
            var roomHeight = bounds.Height - _floorThickness - _ceilingThickness;
            var roomOffsetY = -bounds.Height / 2 + roomHeight / 2 + _floorThickness;
            foreach (var roomInfo in Plan.Rooms)
            {
                var rmBrush = geometry.CreatePrism(hierarchicalParameters.GetValue(new TypedName<string>("material")), roomInfo.Footprint, roomHeight)
                        .Translate(new Vector3(0, roomOffsetY, 0));
                brush = brush.Subtract(rmBrush);
            }

            //Create room scripts (and set them on the tag)
            foreach (var roomInfo in Plan.Rooms)
            {
                var room = CreateChild(new Prism(roomHeight, roomInfo.Footprint), Quaternion.Identity, new Vector3(0, roomOffsetY, 0), roomInfo.Scripts);
                roomInfo.Tag = room;
            }

            //Create room facades
            //throw new NotImplementedException();

            //Union floor into world
            geometry.Union(brush);
        }

        /// <summary>
        /// Call Plan.AddRoom to put new rooms into the floor plan
        /// </summary>
        /// <param name="plan"></param>
        protected abstract void CreateRooms(FloorPlan plan);
    }
}

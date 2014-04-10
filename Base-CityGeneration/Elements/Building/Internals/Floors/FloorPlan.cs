using System;
using System.Collections.Generic;
using System.Linq;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Building.Internals.Floors
{
    public class FloorPlan
    {
        private const float SCALE = 100000;

        private bool _isFrozen = false;
        private readonly Clipper _clipper = new Clipper();

        private readonly Vector2[] _footprint;
        private readonly float _externalWallThickness;

        private readonly List<RoomInfo> _rooms = new List<RoomInfo>();
        public IEnumerable<RoomInfo> Rooms
        {
            get { return _rooms; }
        }

        public FloorPlan(Vector2[] footprint, float externalWallThickness)
        {
            _footprint = footprint;
            _externalWallThickness = externalWallThickness;
        }

        public void Freeze()
        {
            _isFrozen = true;
        }

        public IEnumerable<RoomInfo> AddRoom(IEnumerable<Vector2> roomFootprint, float wallThickness, IEnumerable<ScriptReference> scripts, bool split = false)
        {
            if (_isFrozen)
                throw new InvalidOperationException("Cannot add rooms to floorplan once it is frozen");

            //Contain room inside floor
            _clipper.Clear();
            _clipper.AddPolygon(roomFootprint.Shrink(wallThickness).Select(ToPoint).ToList(), PolyType.Subject);
            _clipper.AddPolygon(_footprint.Shrink(_externalWallThickness).Select(ToPoint).ToList(), PolyType.Clip);
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            _clipper.Execute(ClipType.Intersection, solution);

            if (solution.Count > 1 && !split)
                return new RoomInfo[0];

            //Clip against other rooms
            if (_rooms.Count > 0)
            {
                _clipper.Clear();
                _clipper.AddPolygons(solution, PolyType.Subject);
                _clipper.AddPolygons(_rooms.Select(r => r.Footprint.Shrink(-r.WallThickness).Select(ToPoint).ToList()).ToList(), PolyType.Clip);
                solution.Clear();
                _clipper.Execute(ClipType.Difference, solution, PolyFillType.EvenOdd, PolyFillType.EvenOdd);

                if (solution.Count > 1 && !split)
                    return new RoomInfo[0];
            }

            var s = scripts.ToArray();

            List<RoomInfo> result = new List<RoomInfo>();
            foreach (var shape in solution)
            {
                var r  = new RoomInfo(shape.Select(ToVector2).ToArray(), wallThickness, s);
                result.Add(r);
                _rooms.Add(r);
            }

            return result;
        }

        public IEnumerable<RoomInfo> FindsNeighbours(RoomInfo room)
        {
            throw new NotImplementedException();
        }

        #region static helpers
        private static IntPoint ToPoint(Vector2 v)
        {
            return new IntPoint((int)(v.X * SCALE), (int)(v.Y * SCALE));
        }

        private static Vector2 ToVector2(IntPoint v)
        {
            return new Vector2(v.X / SCALE, v.Y / SCALE);
        }
        #endregion

        public class RoomInfo
        {
            public readonly Vector2[] Footprint;
            public readonly ScriptReference[] Scripts;
            public readonly float WallThickness;

            public object Tag;

            public RoomInfo(Vector2[] footprint, float wallThickness, ScriptReference[] scripts)
            {
                Footprint = footprint;
                Scripts = scripts;
                WallThickness = wallThickness;
            }
        }
    }
}

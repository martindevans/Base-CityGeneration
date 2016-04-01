using System.Numerics;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan
{
    public class Neighbour
    {
        /// <summary>
        /// The index of the edge on roomAB
        /// </summary>
        public uint EdgeIndexRoomAB { get; private set; }

        /// <summary>
        /// One of the rooms in a neighbourship pair (touching points A and B)
        /// </summary>
        public IRoomPlan RoomAB { get; private set; }

        /// <summary>
        /// The index of the edge on roomCD
        /// </summary>
        public uint EdgeIndexRoomCD { get; private set; }

        /// <summary>
        /// One of the rooms in a neighbourship pair (touching points C and D)
        /// </summary>
        public IRoomPlan RoomCD { get; private set; }

        /// <summary>
        /// The first point on the border of these two rooms
        /// </summary>
        public Vector2 A { get; private set; }

        /// <summary>
        /// The second point on the border of these two rooms
        /// </summary>
        public Vector2 B { get; private set; }

        /// <summary>
        /// The third point on the border of these two rooms
        /// </summary>
        public Vector2 C { get; private set; }

        /// <summary>
        /// The fourth point on the border of these two rooms
        /// </summary>
        public Vector2 D { get; private set; }

        /// <summary>
        /// Distance along Edge AB to point A
        /// </summary>
        public float At { get; private set; }

        /// <summary>
        /// Distance along Edge AB to point B
        /// </summary>
        public float Bt { get; private set; }

        /// <summary>
        /// Distance along Edge CD to point C
        /// </summary>
        public float Ct { get; private set; }

        /// <summary>
        /// Distance along Edge CD to point D
        /// </summary>
        public float Dt { get; private set; }

        public Neighbour(uint edgeAbIndex, IRoomPlan ab, uint edgeCdIndex, IRoomPlan cd, Vector2 a, float at, Vector2 b, float bt, Vector2 c, float ct, Vector2 d, float dt)
        {
            EdgeIndexRoomAB = edgeAbIndex;
            EdgeIndexRoomCD = edgeCdIndex;

            RoomAB = ab;
            RoomCD = cd;

            A = a;
            B = b;
            C = c;
            D = d;

            At = at;
            Bt = bt;
            Ct = ct;
            Dt = dt;
        }

        public IRoomPlan Other(IRoomPlan room)
        {
            if (RoomAB == room)
                return RoomCD;
            else
                return RoomAB;
        }

        public LineSegment2 Segment(IRoomPlan room)
        {
            if (RoomAB == room)
                return new LineSegment2(A, B);
            else
                return new LineSegment2(C, D);
        }
    }
}

//namespace Base_CityGeneration.Elements.Building.Internals.Rooms
//{
//    public interface IDoorPlacer
//    {
//        bool ConnectTo(IPlannedRoom otherRoom);
//    }

//    public static class IDoorPlacerExtensions
//    {
//        /// <summary>
//        /// Attempt to request a door connection between the rooms
//        /// </summary>
//        /// <param name="a"></param>
//        /// <param name="b"></param>
//        /// <returns></returns>
//        public static bool TryConnect(this IPlannedRoom a, IPlannedRoom b)
//        {
//            if (a == null || b == null)
//                return false;

//            var ap = a as IDoorPlacer;
//            var bp = b as IDoorPlacer;

//            var at = a as IDoorTarget;
//            if (at != null && !at.AllowConnectionTo(b))
//                return false;

//            var bt = b as IDoorTarget;
//            if (bt != null && !bt.AllowConnectionTo(a))
//                return false;

//            return (ap != null && ap.ConnectTo(b)) || (bp != null && bp.ConnectTo(a));
//        }
//    }
//}

//using Base_CityGeneration.Datastructures.HalfEdge;
//using EpimetheusPlugins.Procedural.Utilities;

//namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan
//{
//    /// <summary>
//    /// Base interface for tags on floorplan tags
//    /// </summary>
//    public interface IFaceTag
//        : IFaceTag<IVertexTag, IHalfEdgeTag, IFaceTag>
//    {
//    }

//    /// <summary>
//    /// A section of wall
//    /// </summary>
//    public interface IWallSectionTag
//        : IFaceTag
//    {
//        Walls.Section Section { get; }
//    }

//    /// <summary>
//    /// A room
//    /// </summary>
//    public interface IRoomTag
//        : IFaceTag
//    {

//    }


//    public abstract class BaseFaceTag
//        : BaseFaceTag<IVertexTag, IHalfEdgeTag, IFaceTag>, IFaceTag
//    {
//        protected BaseFaceTag()
//        {
//        }
//    }

//    public class RoomFaceTag
//        : BaseFaceTag, IRoomTag
//    {
//        public RoomFaceTag(bool walkable, bool entryway)
//        {
            
//        }
//    }

//    public class WallSectionFaceTag
//        : BaseFaceTag, IWallSectionTag
//    {
//        public Walls.Section Section { get; private set; }

//        public WallSectionFaceTag(Walls.Section section)
//        {
//            Section = section;
//        }
//    }
//}

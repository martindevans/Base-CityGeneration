namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning
{
    public class FloorplanVertexTag
    {
    }

    public class FloorplanHalfEdgeTag
    {
        public bool IsImpassable { get; private set; }

        public FloorplanHalfEdgeTag(bool isImpassable)
        {
            IsImpassable = isImpassable;
        }
    }

    public class FloorplanFaceTag
    {
    }
}

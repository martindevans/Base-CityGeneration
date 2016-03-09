namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning
{
    public class FloorplanVertexTag
    {
    }

    public class FloorplanHalfEdgeTag
    {
        public bool IsExternal { get; private set; }

        public FloorplanHalfEdgeTag(bool isExternal)
        {
            IsExternal = isExternal;
        }
    }

    public class FloorplanFaceTag
    {
    }
}

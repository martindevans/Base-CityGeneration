using Myre;

namespace Base_CityGeneration.Styles
{
    public static class Doors
    {
        /// <summary>
        /// The minimum width of a normal sized door
        /// </summary>
        public static readonly TypedNameDefault<float> MinimumStandardDoorWidth = new TypedNameDefault<float>("min_door_width", 0.9f);

        /// <summary>
        /// The maximum width of a normal sized door
        /// </summary>
        public static readonly TypedNameDefault<float> MaximumStandardDoorWidth = new TypedNameDefault<float>("max_door_width", 1.2f);
    }
}

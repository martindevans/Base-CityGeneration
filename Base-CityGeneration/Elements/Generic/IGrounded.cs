namespace Base_CityGeneration.Elements.Generic
{
    /// <summary>
    /// A element which is attached to the ground
    /// </summary>
    public interface IGrounded
    {
        /// <summary>
        /// THe height of the ground (in the local space of this node)
        /// </summary>
        float GroundHeight { get; set; }
    }
}

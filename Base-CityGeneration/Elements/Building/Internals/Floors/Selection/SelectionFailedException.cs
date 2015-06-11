using System;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection
{
    public class SelectionFailedException
        : InvalidOperationException
    {
        public SelectionFailedException(string message)
            : base(message)
        {
        }
    }
}

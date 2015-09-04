using System;

namespace Base_CityGeneration.Elements.Building.Design
{
    public class DesignFailedException
        : InvalidOperationException
    {
        public DesignFailedException(string message)
            : base(message)
        {
        }
    }
}

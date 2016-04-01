using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Numerics;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan
{
    [ContractClass(typeof(IFloorPlanBuilderContract))]
    public interface IFloorPlanBuilder
        : IFloorPlan
    {
        IReadOnlyList<IRoomPlan> Add(IEnumerable<Vector2> roomFootprint, float wallThickness, bool split = false);

        IFloorPlan Freeze();
    }

    [ContractClassFor(typeof(IFloorPlanBuilder))]
    internal abstract class IFloorPlanBuilderContract : IFloorPlanBuilder
    {
        public abstract IReadOnlyList<Vector2> ExternalFootprint { get; }
        public abstract IEnumerable<IRoomPlan> Rooms { get; }

        public abstract IReadOnlyList<IReadOnlyList<Vector2>> TestRoom(IEnumerable<Vector2> roomFootprint, bool split = false);

        public IReadOnlyList<IRoomPlan> Add(IEnumerable<Vector2> roomFootprint, float wallThickness, bool split = false)
        {
            Contract.Requires(roomFootprint != null);
            Contract.Requires(wallThickness > 0);
            Contract.Ensures(Contract.Result<IReadOnlyList<IRoomPlan>>() != null);

            return null;
        }

        public IFloorPlan Freeze()
        {
            Contract.Ensures(Contract.Result<IFloorPlan>() != null);
            return null;
        }
    }

    [ContractClass(typeof(IFloorPlanContract))]
    public interface IFloorPlan
    {
        IReadOnlyList<Vector2> ExternalFootprint { get; }
        IEnumerable<IRoomPlan> Rooms { get; }

        IReadOnlyList<IReadOnlyList<Vector2>> TestRoom(IEnumerable<Vector2> roomFootprint, bool split = false);
    }

    [ContractClassFor(typeof(IFloorPlan))]
    internal abstract class IFloorPlanContract : IFloorPlan
    {
        public IReadOnlyList<Vector2> ExternalFootprint
        {
            get
            {
                Contract.Ensures(Contract.Result<IReadOnlyList<Vector2>>() != null);
                return null;
            }
        }

        public IEnumerable<IRoomPlan> Rooms
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<IRoomPlan>>() != null);
                return null;
            }
        }

        public IReadOnlyList<IReadOnlyList<Vector2>> TestRoom(IEnumerable<Vector2> roomFootprint, bool split = false)
        {
            Contract.Requires(roomFootprint != null);
            Contract.Ensures(Contract.Result<IReadOnlyList<IReadOnlyList<Vector2>>>() != null);

            return null;
        }
    }
}

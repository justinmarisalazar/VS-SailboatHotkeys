using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SailboatHotkeys.Client
{
    public sealed class BoatMountTracker(ILogger logger, ICoreClientAPI clientApi)
    {
        private const long NoBoatMounted = -1;
        private readonly ILogger logger = logger;
        private readonly ICoreClientAPI clientApi = clientApi;
        private long boatEntityId = NoBoatMounted;
        private IMountableSeat mountedSeat = null;

        public long BoatEntityId => boatEntityId;

        public bool HasMountedBoat => boatEntityId != NoBoatMounted;

        public IMountableSeat MountedSeat => mountedSeat;

        public void OnEntityMounted(EntityAgent mountingEntity, IMountableSeat mountedSeat)
        {
            if (
                mountedSeat.MountSupplier.OnEntity is EntityBoat boat
                && mountingEntity.EntityId == clientApi.World.Player.Entity.EntityId
            )
            {
                boatEntityId = boat.EntityId;
                this.mountedSeat = mountedSeat;
                logger.Debug(
                    $"Set boatEntityId to: {boatEntityId} and mountedSeatId to: {mountedSeat.SeatId}"
                );
            }
        }

        public void OnEntityUnmounted(EntityAgent mountingEntity, IMountableSeat mountedSeat)
        {
            if (
                mountedSeat.MountSupplier.OnEntity is EntityBoat
                && mountingEntity.EntityId == clientApi.World.Player.Entity.EntityId
            )
            {
                boatEntityId = NoBoatMounted;
                this.mountedSeat = null;
                logger.Debug("Unset boatEntityId and mountedSeat");
            }
        }
    }
}

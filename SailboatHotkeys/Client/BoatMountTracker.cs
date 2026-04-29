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

        public long BoatEntityId => boatEntityId;

        public bool HasMountedBoat => boatEntityId != NoBoatMounted;

        public void OnEntityMounted(EntityAgent mountingEntity, IMountableSeat mountedSeat)
        {
            if (
                mountedSeat.MountSupplier.OnEntity is EntityBoat boat
                && mountingEntity.EntityId == clientApi.World.Player.Entity.EntityId
            )
            {
                boatEntityId = boat.EntityId;
                logger.Debug($"Set boatEntityId to: {boatEntityId}");
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
                logger.Debug("Unset boatEntityId and mountedSeatId");
            }
        }
    }
}

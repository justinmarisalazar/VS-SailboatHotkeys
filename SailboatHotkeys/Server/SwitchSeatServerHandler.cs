using System.Linq;
using SailboatHotkeys.Networking;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace SailboatHotkeys.Server
{
    public sealed class SwitchSeatServerHandler(ICoreServerAPI serverApi, ILogger logger)
    {
        private readonly ICoreServerAPI serverApi = serverApi;
        private readonly ILogger logger = logger;

        public void HandlePacket(IServerPlayer fromPlayer, SwitchSeatPacket packet)
        {
            if (serverApi.World.GetEntityById(packet.BoatEntityId) is not EntityBoat boatEntity)
            {
                logger.Warning(
                    "Received seat switch packet but no such boat entity exists on the server"
                        + $"(id: {packet.BoatEntityId}, player: {fromPlayer.PlayerName})"
                );
                return;
            }

            if (!TryGetTargetSeat(boatEntity, packet.TargetSeatId, out IMountableSeat targetSeat))
            {
                logger.Warning(
                    "Received seat switch packet but no such seat exists on the boat"
                        + $"(boat id: {packet.BoatEntityId}, seat id: {packet.TargetSeatId}, "
                        + $"player: {fromPlayer.PlayerName})"
                );
                return;
            }

            if (fromPlayer.Entity.TryMount(targetSeat))
            {
                logger.Debug(
                    $"Switched seat (player: {fromPlayer.PlayerName}, target seat: {packet.TargetSeatId}, "
                        + $"boat id: {packet.BoatEntityId})"
                );
            }
            else
            {
                logger.Warning(
                    "Failed to switch seat for unknown reason (TryMount returned false)"
                        + $"(player: {fromPlayer.PlayerName}, target seat: {packet.TargetSeatId}, "
                        + $"boat id: {packet.BoatEntityId})"
                );
            }
        }

        private static bool TryGetTargetSeat(
            EntityBoat boatEntity,
            string targetSeatId,
            out IMountableSeat targetSeat
        )
        {
            IMountableSeat[] seats = boatEntity.GetBehavior<EntityBehaviorSeatable>().Seats;
            if (
                seats.FirstOrDefault(seat => seat.SeatId == targetSeatId)
                is IMountableSeat foundSeat
            )
            {
                targetSeat = foundSeat;
                return true;
            }

            targetSeat = null;
            return false;
        }
    }
}

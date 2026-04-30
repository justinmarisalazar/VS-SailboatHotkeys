using System;
using SailboatHotkeys.Networking;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace SailboatHotkeys.Client
{
    public sealed class ChangeSeatClientSync(IClientNetworkChannel clientChannel, ILogger logger)
    {
        private readonly IClientNetworkChannel clientChannel = clientChannel;
        private readonly ILogger logger = logger;

        public void Sync(IMountableSeat targetSeat)
        {
            var packet = new ChangeSeatPacket
            {
                BoatEntityId = targetSeat.MountSupplier.OnEntity.EntityId,
                TargetSeatId = targetSeat.SeatId,
            };

            try
            {
                clientChannel.SendPacket(packet);
            }
            catch (Exception e)
            {
                logger.Error(
                    $"Failed to synchronize seat switch for target seat {packet.TargetSeatId} on boat "
                        + $"{packet.BoatEntityId} due to exception: {e}"
                );
            }
        }
    }
}

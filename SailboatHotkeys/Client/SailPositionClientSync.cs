using System;
using SailboatHotkeys.Networking;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace SailboatHotkeys.Client
{
    public sealed class SailPositionClientSync(IClientNetworkChannel clientChannel, ILogger logger)
    {
        private readonly IClientNetworkChannel clientChannel = clientChannel;
        private readonly ILogger logger = logger;
        private int lastSentSailPosition = -1;

        public void Reset()
        {
            lastSentSailPosition = -1;
        }

        public void SyncIfChanged(long boatEntityId, int targetSailPosition)
        {
            if (targetSailPosition == lastSentSailPosition)
            {
                return;
            }

            Sync(boatEntityId, targetSailPosition);
            lastSentSailPosition = targetSailPosition;
        }

        private void Sync(long boatEntityId, int targetSailPosition)
        {
            var packet = new SailPositionPacket
            {
                BoatEntityId = boatEntityId,
                TargetSailPosition = targetSailPosition,
            };

            try
            {
                clientChannel.SendPacket(packet);
            }
            catch (Exception e)
            {
                logger.Error(
                    $"Failed to synchronize sail position: boat entity with id {packet.BoatEntityId} due to "
                        + $"exception: {e}"
                );
            }
        }
    }
}

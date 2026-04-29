using SailboatHotkeys.Networking;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace SailboatHotkeys.Server
{
    public sealed class SailPositionServerHandler(ICoreServerAPI serverApi, ILogger logger)
    {
        private readonly ICoreServerAPI serverApi = serverApi;
        private readonly ILogger logger = logger;

        public void HandlePacket(IServerPlayer fromPlayer, SailPositionPacket packet)
        {
            if (serverApi.World.GetEntityById(packet.BoatEntityId) is not EntityBoat boatEntity)
            {
                logger.Warning(
                    "Received sail position packet but no such boat entity exists on the server"
                        + $"(id: {packet.BoatEntityId}, player: {fromPlayer.PlayerName})"
                );
                return;
            }

            boatEntity.WatchedAttributes.SetInt("sailPosition", packet.TargetSailPosition);
            logger.Debug(
                $"Synchronized sail position (position: {packet.TargetSailPosition}, "
                    + $"boat id: {packet.BoatEntityId}, player: {fromPlayer.PlayerName})"
            );
        }
    }
}

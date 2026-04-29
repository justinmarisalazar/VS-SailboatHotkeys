using SailboatHotkeys.Client;
using SailboatHotkeys.Networking;
using SailboatHotkeys.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace SailboatHotkeys
{
    public class SailboatHotkeysModSystem : ModSystem
    {
        private const string ChannelName = "sailboatHotkeys";

        private ICoreClientAPI clientApi;
        private BoatMountTracker boatMountTracker;
        private SailPositionClientSync sailPositionClientSync;
        private SailHotkeyController sailHotkeyController;
        private SailPositionServerHandler sailPositionServerHandler;

        public override void Start(ICoreAPI api)
        {
            api.Network.RegisterChannel(ChannelName).RegisterMessageType<SailPositionPacket>();
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("SailboatHotkeys Mod System started");

            clientApi = api;

            boatMountTracker = new BoatMountTracker(Mod.Logger, api);
            sailPositionClientSync = new SailPositionClientSync(
                api.Network.GetChannel(ChannelName),
                Mod.Logger
            );
            sailHotkeyController = new SailHotkeyController(
                api,
                boatMountTracker,
                sailPositionClientSync,
                Mod.Logger
            );

            api.Event.EntityMounted += boatMountTracker.OnEntityMounted;
            api.Event.EntityUnmounted += boatMountTracker.OnEntityUnmounted;
            sailHotkeyController.RegisterHotkeys();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("SailboatHotkeys Mod System started");
            sailPositionServerHandler = new SailPositionServerHandler(api, Mod.Logger);
            api.Network.GetChannel(ChannelName)
                .SetMessageHandler<SailPositionPacket>(sailPositionServerHandler.HandlePacket);
        }

        public override void Dispose()
        {
            if (clientApi != null && boatMountTracker != null)
            {
                clientApi.Event.EntityMounted -= boatMountTracker.OnEntityMounted;
                clientApi.Event.EntityUnmounted -= boatMountTracker.OnEntityUnmounted;
            }

            sailPositionClientSync?.Reset();
            base.Dispose();
        }
    }
}

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
        private SwitchSeatClientSync switchSeatClientSync;
        private SailHotkeyController sailHotkeyController;
        private SailPositionServerHandler sailPositionServerHandler;
        private SwitchSeatServerHandler switchSeatServerHandler;

        public override void Start(ICoreAPI api)
        {
            api.Network.RegisterChannel(ChannelName)
                .RegisterMessageType<SailPositionPacket>()
                .RegisterMessageType<SwitchSeatPacket>();
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
            switchSeatClientSync = new SwitchSeatClientSync(
                api.Network.GetChannel(ChannelName),
                Mod.Logger
            );
            sailHotkeyController = new SailHotkeyController(
                api,
                boatMountTracker,
                sailPositionClientSync,
                switchSeatClientSync,
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
            switchSeatServerHandler = new SwitchSeatServerHandler(api, Mod.Logger);
            api.Network.GetChannel(ChannelName)
                .SetMessageHandler<SailPositionPacket>(sailPositionServerHandler.HandlePacket)
                .SetMessageHandler<SwitchSeatPacket>(switchSeatServerHandler.HandlePacket);
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

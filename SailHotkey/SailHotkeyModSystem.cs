using System;
using System.Reflection;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace SailHotkey
{
    [ProtoContract]
    public class SailPositionPacket
    {
        [ProtoMember(1)]
        public long BoatEntityId;

        [ProtoMember(2)]
        public int TargetSailPosition;
    }

    public class SailHotkeyModSystem : ModSystem
    {
        // Called on server and client
        public override void Start(ICoreAPI api)
        {
            api.Network.RegisterChannel("sailhotkey").RegisterMessageType<SailPositionPacket>();
        }

        #region Client

        ICoreClientAPI ccapi;
        IClientNetworkChannel clientChannel;
        long boatEntityId = -1;

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Sail Hotkey Mod System started");

            api.Input.RegisterHotKey(
                "increaseSailPosition",
                "Increase Sail Position",
                GlKeys.W,
                HotkeyType.MovementControls,
                ctrlPressed: true
            );
            api.Input.SetHotKeyHandler("increaseSailPosition", OnIncreaseSailPositionHotkeyPressed);
            api.Input.RegisterHotKey(
                "decreaseSailPosition",
                "Decrease Sail Position",
                GlKeys.S,
                HotkeyType.MovementControls,
                ctrlPressed: true
            );
            api.Input.SetHotKeyHandler("decreaseSailPosition", OnDecreaseSailPositionHotkeyPressed);

            api.Event.EntityMounted += SetBoatEntityId;
            api.Event.EntityUnmounted += UnsetBoatEntityId;

            clientChannel = api.Network.GetChannel("sailhotkey");
            ccapi = api;
        }

        private bool OnIncreaseSailPositionHotkeyPressed(KeyCombination keyCombo)
        {
            Mod.Logger.Debug(
                "Increase sail position hotkey pressed with key combination: " + keyCombo
            );
            if (boatEntityId != -1)
            {
                int targetSailPosition = IncreaseSailPosition();
                SyncSailPositionWithServer(
                    new SailPositionPacket
                    {
                        BoatEntityId = boatEntityId,
                        TargetSailPosition = targetSailPosition,
                    }
                );
            }
            else
                Mod.Logger.Debug("boatEntityId is not set, skipping hotkey action");
            return true; // Return true to indicate that the hotkey was handled
        }

        private bool OnDecreaseSailPositionHotkeyPressed(KeyCombination keyCombo)
        {
            Mod.Logger.Debug(
                "Decrease sail position hotkey pressed with key combination: " + keyCombo
            );
            if (boatEntityId != -1)
            {
                int targetSailPosition = DecreaseSailPosition();
                SyncSailPositionWithServer(
                    new SailPositionPacket
                    {
                        BoatEntityId = boatEntityId,
                        TargetSailPosition = targetSailPosition,
                    }
                );
            }
            else
                Mod.Logger.Debug("boatEntityId is not set, skipping hotkey action");
            return true; // Return true to indicate that the hotkey was handled
        }

        private int IncreaseSailPosition()
        {
            EntityBoat boatEntity = ccapi.World.GetEntityById(boatEntityId) as EntityBoat;
            int currentSailPosition = boatEntity.WatchedAttributes.GetInt("sailPosition");
            int targetSailPosition = Math.Min(currentSailPosition + 1, 2);
            boatEntity.WatchedAttributes.SetInt("sailPosition", targetSailPosition);
            Mod.Logger.Debug(
                $"Set sail position to: {targetSailPosition} for boat with id: {boatEntityId}"
            );
            return targetSailPosition;
        }

        private int DecreaseSailPosition()
        {
            EntityBoat boatEntity = ccapi.World.GetEntityById(boatEntityId) as EntityBoat;
            int currentSailPosition = boatEntity.WatchedAttributes.GetInt("sailPosition");
            int targetSailPosition = Math.Max(currentSailPosition - 1, 0);
            boatEntity.WatchedAttributes.SetInt("sailPosition", targetSailPosition);
            Mod.Logger.Debug(
                $"Set sail position to: {targetSailPosition} for boat with id: {boatEntityId}"
            );
            return targetSailPosition;
        }

        #endregion Client

        #region Server

        ICoreServerAPI scapi;

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Sail Hotkey Mod System started");
            api.Network.GetChannel("sailhotkey")
                .SetMessageHandler<SailPositionPacket>(OnServerReceiveSailPositionPacket);
            scapi = api;
        }

        private void SyncSailPositionWithServer(SailPositionPacket packet)
        {
            try
            {
                clientChannel.SendPacket(packet);
            }
            catch (Exception e)
            {
                Mod.Logger.Error(
                    $"Failed to synchronize sail position: boat entity with id {packet.BoatEntityId} due to "
                        + $"exception: {e}"
                );
            }
        }

        private void SetBoatEntityId(EntityAgent mountingEntity, IMountableSeat mountedSeat)
        {
            if (mountedSeat.MountSupplier.OnEntity is EntityBoat)
            {
                boatEntityId = mountedSeat.MountSupplier.OnEntity.EntityId;
                Mod.Logger.Debug($"Set boatEntityId to: {boatEntityId}");
            }
        }

        private void UnsetBoatEntityId(EntityAgent mountingEntity, IMountableSeat mountedSeat)
        {
            if (mountedSeat.MountSupplier.OnEntity is EntityBoat)
            {
                boatEntityId = -1;
                Mod.Logger.Debug("Unset boatEntityId");
            }
        }

        private void OnServerReceiveSailPositionPacket(
            IServerPlayer fromPlayer,
            SailPositionPacket packet
        )
        {
            if (scapi.World.GetEntityById(packet.BoatEntityId) is not EntityBoat boatEntity)
            {
                Mod.Logger.Warning(
                    $"Received sail position packet but no such boat entity exists on the server"
                        + $"(id: {packet.BoatEntityId}, player: {fromPlayer.PlayerName})"
                );
                return;
            }

            boatEntity.WatchedAttributes.SetInt("sailPosition", packet.TargetSailPosition);
            Mod.Logger.Debug(
                $"Synchronized sail position (position: {packet.TargetSailPosition}) for boat with "
                    + $"id: {packet.BoatEntityId} from player: {fromPlayer.PlayerName}"
            );
        }

        #endregion Server
    }
}

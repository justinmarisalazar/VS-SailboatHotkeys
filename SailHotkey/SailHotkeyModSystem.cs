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
    public class SailHotkeyModSystem : ModSystem
    {
        ICoreServerAPI scapi;
        ICoreClientAPI ccapi;

        IClientNetworkChannel clientChannel;

        long boatEntityId = -1;

        // Called on server and client
        public override void Start(ICoreAPI api)
        {
            api.Network.RegisterChannel("sailhotkey").RegisterMessageType<SailPositionPacket>();
            // TODO: test in core api
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Sail Hotkey Mod System started");
            api.Network.GetChannel("sailhotkey")
                .SetMessageHandler<SailPositionPacket>(OnServerReceivePacket);
            scapi = api;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Sail Hotkey Mod System started");

            api.Input.RegisterHotKey(
                "increaseSailSpeed",
                "Increase Sail Speed",
                GlKeys.W,
                HotkeyType.GUIOrOtherControls,
                ctrlPressed: true
            );
            api.Input.SetHotKeyHandler("increaseSailSpeed", OnIncreaseSailSpeedHotkeyPressed);
            api.Input.RegisterHotKey(
                "decreaseSailSpeed",
                "Decrease Sail Speed",
                GlKeys.S,
                HotkeyType.GUIOrOtherControls,
                ctrlPressed: true
            );
            api.Input.SetHotKeyHandler("decreaseSailSpeed", OnDecreaseSailSpeedHotkeyPressed);

            api.Event.EntityMounted += SetBoatEntityId;
            api.Event.EntityUnmounted += UnsetBoatEntityId;

            clientChannel = api.Network.GetChannel("sailhotkey");
            ccapi = api;
        }

        private bool OnIncreaseSailSpeedHotkeyPressed(KeyCombination keyCombo)
        {
            Mod.Logger.Debug(
                "Increase sail speed hotkey pressed with key combination: " + keyCombo
            );
            if (boatEntityId != -1)
            {
                SailPositionPacket packet = IncreaseSailPosition();
                SyncSailPositionWithServer(packet);
            }
            else
                Mod.Logger.Debug("boatEntityId is not set, skipping hotkey action");
            return true; // Return true to indicate that the hotkey was handled
        }

        private bool OnDecreaseSailSpeedHotkeyPressed(KeyCombination keyCombo)
        {
            Mod.Logger.Debug(
                "Decrease sail speed hotkey pressed with key combination: " + keyCombo
            );
            if (boatEntityId != -1)
            {
                SailPositionPacket packet = DecreaseSailPosition();
                SyncSailPositionWithServer(packet);
            }
            else
                Mod.Logger.Debug("boatEntityId is not set, skipping hotkey action");
            return true; // Return true to indicate that the hotkey was handled
        }

        private SailPositionPacket IncreaseSailPosition()
        {
            EntityBoat boatEntity = ccapi.World.GetEntityById(boatEntityId) as EntityBoat;
            int currentSailPosition = boatEntity.WatchedAttributes.GetInt("sailPosition");
            int targetSailPosition = Math.Min(currentSailPosition + 1, 2);
            boatEntity.WatchedAttributes.SetInt("sailPosition", targetSailPosition);
            Mod.Logger.Debug(
                "Set sail position to: " + targetSailPosition + " for boat with id: " + boatEntityId
            );
            return new SailPositionPacket
            {
                BoatEntityId = boatEntityId,
                TargetSailPosition = targetSailPosition,
            };
        }

        private SailPositionPacket DecreaseSailPosition()
        {
            EntityBoat boatEntity = ccapi.World.GetEntityById(boatEntityId) as EntityBoat;
            int currentSailPosition = boatEntity.WatchedAttributes.GetInt("sailPosition");
            int targetSailPosition = Math.Max(currentSailPosition - 1, 0);
            boatEntity.WatchedAttributes.SetInt("sailPosition", targetSailPosition);
            Mod.Logger.Debug(
                "Set sail position to: " + targetSailPosition + " for boat with id: " + boatEntityId
            );
            return new SailPositionPacket
            {
                BoatEntityId = boatEntityId,
                TargetSailPosition = targetSailPosition,
            };
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
                    "Failed to synchronize sail position: boat entity with id "
                        + packet.BoatEntityId
                        + " due to exception: "
                        + e.ToString()
                );
            }
        }

        private void SetBoatEntityId(EntityAgent mountingEntity, IMountableSeat mountedSeat)
        {
            if (mountedSeat.MountSupplier.OnEntity is EntityBoat)
            {
                boatEntityId = mountedSeat.MountSupplier.OnEntity.EntityId;
                Mod.Logger.Debug("Set boatEntityId to: " + boatEntityId);
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

        private void OnServerReceivePacket(IServerPlayer fromPlayer, SailPositionPacket packet)
        {
            if (scapi.World.GetEntityById(packet.BoatEntityId) is not EntityBoat boatEntity)
            {
                return;
            }

            boatEntity.WatchedAttributes.SetInt("sailPosition", packet.TargetSailPosition);
            Mod.Logger.Debug(
                "Synchronized sail position to: "
                    + packet.TargetSailPosition
                    + " for boat with id: "
                    + packet.BoatEntityId
            );
        }
    }

    [ProtoContract]
    public class SailPositionPacket
    {
        [ProtoMember(1)]
        public long BoatEntityId;

        [ProtoMember(2)]
        public int TargetSailPosition;
    }
}

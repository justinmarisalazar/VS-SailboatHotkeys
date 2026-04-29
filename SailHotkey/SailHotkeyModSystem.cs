using System;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
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

        private const string MaxSailPositionHotkeyCode = "maxSailPosition";
        private const string MinSailPositionHotkeyCode = "minSailPosition";
        private const string FurlUnfurlSailsHotkeyCode = "furlUnfurlSails";
        private static readonly int[] ValidSailPositions = [0, 1, 2];
        private static readonly int MaxSailPosition = ValidSailPositions.Max();
        private static readonly int MinSailPosition = ValidSailPositions.Min();

        private ICoreClientAPI ccapi;
        private IClientNetworkChannel clientChannel;
        private long boatEntityId = -1;
        private int lastSentSailPosition = -1; // track the last sent sail position to avoid redundant packets

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Sail Hotkey Mod System started");

            api.Input.RegisterHotKey(
                MaxSailPositionHotkeyCode,
                "Set Max Sail Speed",
                GlKeys.W,
                HotkeyType.HelpAndOverlays,
                ctrlPressed: true
            );
            api.Input.SetHotKeyHandler(MaxSailPositionHotkeyCode, OnMaxSailPositionHotkeyPressed);
            api.Input.RegisterHotKey(
                MinSailPositionHotkeyCode,
                "Set Min Sail Speed",
                GlKeys.S,
                HotkeyType.HelpAndOverlays,
                ctrlPressed: true
            );
            api.Input.SetHotKeyHandler(MinSailPositionHotkeyCode, OnMinSailPositionHotkeyPressed);
            api.Input.RegisterHotKey(
                FurlUnfurlSailsHotkeyCode,
                "Furl/Unfurl Sails",
                GlKeys.V,
                HotkeyType.HelpAndOverlays
            );
            api.Input.SetHotKeyHandler(FurlUnfurlSailsHotkeyCode, OnFurlUnfurlSailsHotkeyPressed);

            api.Event.EntityMounted += SetBoatEntityId;
            api.Event.EntityUnmounted += UnsetBoatEntityId;

            clientChannel = api.Network.GetChannel("sailhotkey");
            ccapi = api;
        }

        private bool OnMaxSailPositionHotkeyPressed(KeyCombination keyCombo)
        {
            if (boatEntityId != -1)
            {
                Mod.Logger.Debug(
                    $"{MaxSailPositionHotkeyCode} hotkey pressed with key combination: {keyCombo}"
                );
                EntityBoat boatEntity = ccapi.World.GetEntityById(boatEntityId) as EntityBoat;
                boatEntity.WatchedAttributes.SetInt("sailPosition", MaxSailPosition);
                if (MaxSailPosition != lastSentSailPosition)
                {
                    SyncSailPositionWithServer(
                        new SailPositionPacket
                        {
                            BoatEntityId = boatEntityId,
                            TargetSailPosition = MaxSailPosition,
                        }
                    );
                    lastSentSailPosition = MaxSailPosition;
                }
                Mod.Logger.Debug(
                    $"Set sail position to max ({MaxSailPosition}) for boat with id: {boatEntityId}"
                );
            }
            return true; // Return true to indicate that the hotkey was handled
        }

        private bool OnMinSailPositionHotkeyPressed(KeyCombination keyCombo)
        {
            if (boatEntityId != -1)
            {
                Mod.Logger.Debug(
                    $"{MinSailPositionHotkeyCode} hotkey pressed with key combination: {keyCombo}"
                );
                EntityBoat boatEntity = ccapi.World.GetEntityById(boatEntityId) as EntityBoat;
                boatEntity.WatchedAttributes.SetInt("sailPosition", MinSailPosition);
                if (MinSailPosition != lastSentSailPosition)
                {
                    SyncSailPositionWithServer(
                        new SailPositionPacket
                        {
                            BoatEntityId = boatEntityId,
                            TargetSailPosition = MinSailPosition,
                        }
                    );
                    lastSentSailPosition = MinSailPosition;
                }
                Mod.Logger.Debug(
                    $"Set sail position to min ({MinSailPosition}) for boat with id: {boatEntityId}"
                );
            }
            return true; // Return true to indicate that the hotkey was handled
        }

        private bool OnFurlUnfurlSailsHotkeyPressed(KeyCombination keyCombo)
        {
            if (boatEntityId != -1)
            {
                Mod.Logger.Debug(
                    $"{FurlUnfurlSailsHotkeyCode} hotkey pressed with key combination: {keyCombo}"
                );
                EntityBoat boatEntity = ccapi.World.GetEntityById(boatEntityId) as EntityBoat;
                int currentSailPosition = boatEntity.WatchedAttributes.GetInt("sailPosition");
                int currentIndex = Array.IndexOf(ValidSailPositions, currentSailPosition);
                if (currentIndex < 0)
                    currentIndex = 0; // fallback if current value is unexpected

                int nextIndex = (currentIndex + 1) % ValidSailPositions.Length;
                int targetSailPosition = ValidSailPositions[nextIndex]; // cycle to the next sail position
                boatEntity.WatchedAttributes.SetInt("sailPosition", targetSailPosition);
                Mod.Logger.Debug(
                    $"Set sail position to: {targetSailPosition} for boat with id: {boatEntityId}"
                );
                SyncSailPositionWithServer(
                    new SailPositionPacket
                    {
                        BoatEntityId = boatEntityId,
                        TargetSailPosition = targetSailPosition,
                    }
                );
                lastSentSailPosition = targetSailPosition;
            }
            return true; // Return true to indicate that the hotkey was handled
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
                lastSentSailPosition = -1;
                Mod.Logger.Debug("Unset boatEntityId and lastSentSailPosition");
            }
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
                $"Synchronized sail position (position: {packet.TargetSailPosition}, "
                    + $"boat id: {packet.BoatEntityId}, player: {fromPlayer.PlayerName})"
            );
        }

        #endregion Server
    }
}

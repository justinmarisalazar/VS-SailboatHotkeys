using System;
using System.Linq;
using SailboatHotkeys.Domain;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SailboatHotkeys.Client
{
    public sealed class SailHotkeyController(
        ICoreClientAPI clientApi,
        BoatMountTracker boatMountTracker,
        SailPositionClientSync sailPositionSync,
        ILogger logger
    )
    {
        private const string MaxSailPositionHotkeyCode = "maxSailPosition";
        private const string MinSailPositionHotkeyCode = "minSailPosition";
        private const string FurlUnfurlSailsHotkeyCode = "furlUnfurlSails";
        private const string SwitchSeatHotkeyCode = "switchSeat";

        private readonly ICoreClientAPI clientApi = clientApi;
        private readonly BoatMountTracker boatMountTracker = boatMountTracker;
        private readonly SailPositionClientSync sailPositionSync = sailPositionSync;
        private readonly ILogger logger = logger;

        public void RegisterHotkeys()
        {
            clientApi.Input.RegisterHotKey(
                MaxSailPositionHotkeyCode,
                "[SailboatHotkeys] Fully unfurl sails",
                GlKeys.W,
                HotkeyType.CharacterControls,
                ctrlPressed: true
            );
            clientApi.Input.SetHotKeyHandler(
                MaxSailPositionHotkeyCode,
                OnMaxSailPositionHotkeyPressed
            );

            clientApi.Input.RegisterHotKey(
                MinSailPositionHotkeyCode,
                "[SailboatHotkeys] Fully furl sails",
                GlKeys.S,
                HotkeyType.CharacterControls,
                ctrlPressed: true
            );
            clientApi.Input.SetHotKeyHandler(
                MinSailPositionHotkeyCode,
                OnMinSailPositionHotkeyPressed
            );

            clientApi.Input.RegisterHotKey(
                FurlUnfurlSailsHotkeyCode,
                "[SailboatHotkeys] Furl/Unfurl sails",
                GlKeys.V,
                HotkeyType.CharacterControls
            );
            clientApi.Input.SetHotKeyHandler(
                FurlUnfurlSailsHotkeyCode,
                OnFurlUnfurlSailsHotkeyPressed
            );

            clientApi.Input.RegisterHotKey(
                SwitchSeatHotkeyCode,
                "[SailboatHotkeys] Switch seat",
                GlKeys.B,
                HotkeyType.CharacterControls
            );
            clientApi.Input.SetHotKeyHandler(SwitchSeatHotkeyCode, OnSwitchSeatHotkeyPressed);
        }

        private bool OnMaxSailPositionHotkeyPressed(KeyCombination keyCombo)
        {
            return TryApplySailPosition(
                MaxSailPositionHotkeyCode,
                SailPositionDomain.MaxPosition,
                keyCombo
            );
        }

        private bool OnMinSailPositionHotkeyPressed(KeyCombination keyCombo)
        {
            return TryApplySailPosition(
                MinSailPositionHotkeyCode,
                SailPositionDomain.MinPosition,
                keyCombo
            );
        }

        private bool OnFurlUnfurlSailsHotkeyPressed(KeyCombination keyCombo)
        {
            if (!TryGetMountedBoat(out long boatEntityId, out EntityBoat boatEntity))
            {
                return true;
            }

            logger.Debug(
                $"{FurlUnfurlSailsHotkeyCode} hotkey pressed with key combination: {keyCombo}"
            );

            int currentSailPosition = boatEntity.WatchedAttributes.GetInt("sailPosition");
            int targetSailPosition = SailPositionDomain.GetNextPosition(currentSailPosition);

            boatEntity.WatchedAttributes.SetInt("sailPosition", targetSailPosition);
            sailPositionSync.SyncIfChanged(boatEntityId, targetSailPosition);

            logger.Debug(
                $"Set sail position to: {targetSailPosition} for boat with id: {boatEntityId}"
            );

            return true;
        }

        private bool OnSwitchSeatHotkeyPressed(KeyCombination keyCombo)
        {
            if (!TryGetMountedBoat(out long boatEntityId, out EntityBoat boatEntity))
            {
                return true;
            }

            logger.Debug($"Switch seat hotkey pressed with key combination: {keyCombo}");

            if (
                !TryGetNextFreeSeat(
                    boatMountTracker.MountedSeat.MountSupplier.Seats,
                    boatMountTracker.MountedSeat,
                    out IMountableSeat nextFreeSeat
                )
            )
            {
                logger.Debug("No free seat available to switch to.");
                return true;
            }

            if (clientApi.World.Player.Entity.TryMount(nextFreeSeat))
            {
                logger.Debug($"Switched to seat {nextFreeSeat.SeatId}");
            }

            return true;
        }

        private bool TryApplySailPosition(
            string hotkeyCode,
            int targetSailPosition,
            KeyCombination keyCombo
        )
        {
            if (!TryGetMountedBoat(out long boatEntityId, out EntityBoat boatEntity))
            {
                return true;
            }

            logger.Debug($"{hotkeyCode} hotkey pressed with key combination: {keyCombo}");

            boatEntity.WatchedAttributes.SetInt("sailPosition", targetSailPosition);
            sailPositionSync.SyncIfChanged(boatEntityId, targetSailPosition);

            logger.Debug(
                $"Set sail position to ({targetSailPosition}) for boat with id: {boatEntityId}"
            );

            return true;
        }

        private bool TryGetMountedBoat(out long boatEntityId, out EntityBoat boatEntity)
        {
            boatEntityId = -1;
            boatEntity = null;

            if (!boatMountTracker.HasMountedBoat)
            {
                return false;
            }

            boatEntityId = boatMountTracker.BoatEntityId;
            boatEntity = clientApi.World.GetEntityById(boatEntityId) as EntityBoat;
            return boatEntity != null;
        }

        private static bool TryGetNextFreeSeat(
            IMountableSeat[] seats,
            IMountableSeat currentSeat,
            out IMountableSeat nextFreeSeat
        )
        {
            int currentIndex = Array.IndexOf(seats, currentSeat);
            if (currentIndex < 0)
            {
                nextFreeSeat = null;
                return false;
            }

            for (int i = 1; i < seats.Length; i++)
            {
                int candidateIndex = (currentIndex + i) % seats.Length;
                if (seats[candidateIndex].Passenger == null)
                {
                    nextFreeSeat = seats[candidateIndex];
                    return true;
                }
            }

            nextFreeSeat = null;
            return false;
        }
    }
}

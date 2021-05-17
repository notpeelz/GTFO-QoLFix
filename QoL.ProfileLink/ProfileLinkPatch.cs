using System;
using BepInEx.Configuration;
using CellMenu;
using MTFO.Core;
using QoL.Common.Patches;
using QoL.Common.Cursor;
using SNetwork;
using Steamworks;
using UnityEngine;

namespace QoL.ProfileLink
{
    public class ProfileLinkPatch : MTFOPatch
    {
        private const string PatchName = nameof(ProfileLinkPatch);

        private static ConfigEntry<bool> ConfigEnabled = default!;

        public static ProfileLinkPatch? Instance { get; private set; }

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Lets you open the steam profile of your teammates by clicking on their name."));
        }

        protected override void Apply()
        {
            base.Apply();
            PlayerNameExtPatch.CursorUpdate += this.OnCursorUpdate;
        }

        private void OnCursorUpdate(CM_PageBase page, Vector2 pos, ref RaycastHit2D rayHit, bool hovering, Lazy<SNet_Player?> player)
        {
            page.SetCursorStyle(hovering ? CursorStyle.Hand : CursorStyle.Default);

            // This is necessary so that we don't open steam links while the
            // mouse is unlocked (e.g. while the steam overlay is open).
            if (Cursor.lockState == CursorLockMode.None) return;
            if (!hovering || !Input.GetMouseButtonUp(0)) return;
            if (player.Value == null) return;

            Instance!.LogInfo($"Opening steam profile for {player.Value.NickName} ({player.Value.Lookup})");

            var url = $"https://steamcommunity.com/profiles/{player.Value.Lookup}";
            if (SteamUtils.IsOverlayEnabled())
            {
                SteamFriends.ActivateGameOverlayToWebPage(url);
            }
            else
            {
                Application.OpenURL(url);
            }
        }
    }
}

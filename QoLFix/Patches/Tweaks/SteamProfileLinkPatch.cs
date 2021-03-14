using System;
using BepInEx.Configuration;
using CellMenu;
using QoLFix.Patches.Common;
using QoLFix.Patches.Common.Cursor;
using SNetwork;
using Steamworks;
using UnityEngine;

namespace QoLFix.Patches.Tweaks
{
    public class SteamProfileLinkPatch : Patch
    {
        private const string PatchName = nameof(SteamProfileLinkPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Lets you open the steam profile of your teammates by clicking on their name."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public override void Execute()
        {
            QoLFixPlugin.RegisterPatch<PlayerNameExtPatch>();
            PlayerNameExtPatch.CursorUpdate += this.OnCursorUpdate;
        }

        private void OnCursorUpdate(CM_PageBase page, Vector2 pos, ref RaycastHit2D rayHit, bool hovering, Lazy<SNet_Player> player)
        {
            page.SetCursorStyle(hovering ? CursorStyle.Hand : CursorStyle.Default);

            // This is necessary so that we don't open steam links while the
            // mouse is unlocked.
            if (Cursor.lockState == CursorLockMode.None) return;

            if (!hovering || !Input.GetMouseButtonUp(0)) return;

            Instance.LogInfo($"Opening steam profile for {player.Value.NickName} ({player.Value.Lookup})");

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

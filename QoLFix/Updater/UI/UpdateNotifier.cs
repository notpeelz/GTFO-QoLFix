﻿using System;
using QoLFix.UI;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;

namespace QoLFix.Updater.UI
{
    public class UpdateNotifier : MonoBehaviour
    {
        public UpdateNotifier(IntPtr value)
            : base(value) { }

        private static volatile bool ShouldUpdate;
        private static bool Visible;

        public static void SetNotificationVisibility(bool visible)
        {
            Visible = visible;
            ShouldUpdate = true;
        }

        internal void Update()
        {
            if (UpdateNotification == null) return;
            if (!ShouldUpdate) return;
            ShouldUpdate = false;

            UpdateNotification.SetActive(Visible);
            UIManager.UnlockCursor = Visible;

            if (Visible)
            {
                UpdateText.text = GetUpdateMessage();
            }
        }

        private static Text UpdateText;
        private static GameObject UpdateNotification;

        public static void Initialize()
        {
            ClassInjector.RegisterTypeInIl2Cpp<UpdateNotifier>();
            UIManager.CanvasRoot.AddComponent<UpdateNotifier>();
            CreateNotification();
        }

        private static void CreateNotification()
        {
            UpdateNotification = UIManager.CreatePanel("UpdateNotification", UIManager.CanvasRoot.transform, out var content,
                color: new Color(160 / 255f, 160 / 255f, 160 / 255f));

            UpdateNotification.SetActive(false);
            var panelTransform = UpdateNotification.GetComponent<RectTransform>();

            const float width = 0.125f;
            const float height = 0.1f;
            panelTransform.anchorMin = new Vector2(0.5f - width, 0.5f - height);
            panelTransform.anchorMax = new Vector2(0.5f + width, 0.5f + height);

            // Title bar
            UIManager.CreateTitleBar($"<b>{QoLFixPlugin.ModName}</b> - Update available!", content.transform, out _,
                color: new Color(60 / 255f, 60 / 255f, 60 / 255f));

            // Viewport
            var viewport = UIManager.CreateViewport(content.transform);
            var viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = new Color(44f / 255f, 44f / 255f, 44f / 255f);

            // MessageBox
            var msgBox = GOFactory.CreateObject("MessageBox", viewport.transform,
                out VerticalLayoutGroup msgBoxGroup);

            msgBoxGroup.childAlignment = TextAnchor.UpperLeft;
            msgBoxGroup.childControlHeight = true;
            msgBoxGroup.childControlWidth = true;
            msgBoxGroup.childForceExpandHeight = false;
            msgBoxGroup.childForceExpandWidth = true;
            msgBoxGroup.padding = new RectOffset(3, 3, 3, 3);

            // Message
            var msg = GOFactory.CreateObject("Message", msgBox.transform,
                out VerticalLayoutGroup msgGroup,
                out LayoutElement msgLayout);

            msgGroup.padding = new RectOffset(6, 6, 6, 12);
            msgLayout.flexibleHeight = 5000;

            // Message text
            GOFactory.CreateObject("Text", msg.transform, out UpdateText);

            UpdateText.font = UIManager.DefaultFont;
            UpdateText.alignment = TextAnchor.MiddleCenter;
            UpdateText.supportRichText = true;
            UpdateText.text = GetUpdateMessage();
            UpdateText.fontSize = 15;

            // Buttons
            var buttons = GOFactory.CreateObject("ButtonGroup", msgBox.transform,
                out HorizontalLayoutGroup buttonGroup,
                out LayoutElement buttonLayout);

            buttonGroup.childAlignment = TextAnchor.UpperLeft;
            buttonGroup.childControlHeight = true;
            buttonGroup.childControlWidth = true;
            buttonGroup.childForceExpandHeight = true;
            buttonGroup.childForceExpandWidth = true;
            buttonGroup.spacing = 3;
            buttonLayout.minHeight = 30;
            buttonLayout.flexibleHeight = 1;

            UIManager.CreateButton("Yes", buttons.transform, out _,
                normalColor: Color.green.RGBMultiplied(0.7f),
                pressedColor: Color.green.RGBMultiplied(0.5f),
                highlightedColor: Color.green.RGBMultiplied(0.8f),
                callback: () =>
                {
                    UpdateManager.OpenReleasePage();
                    UIManager.UnlockCursor = false;
                    SetNotificationVisibility(false);
                    Application.Quit();
                });
            UIManager.CreateButton("Not now", buttons.transform, out _,
                callback: () =>
                {
                    UIManager.UnlockCursor = false;
                    SetNotificationVisibility(false);
                });
        }

        private static string GetUpdateMessage()
        {
            return $"{QoLFixPlugin.ModName} was updated to {UpdateManager.GetLatestReleaseName()}\n" +
                "Press 'Yes' to open the download page.";
        }
    }
}

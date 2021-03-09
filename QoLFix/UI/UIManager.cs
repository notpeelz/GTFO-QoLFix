using System;
using QoLFix.Patches.Common.Cursor;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QoLFix.UI
{
    public static partial class UIManager
    {
        public static GameObject CanvasRoot { get; private set; }

        public static bool UnlockCursor
        {
            get => unlockCursor;
            set
            {
                unlockCursor = value;
                UnityCursorPatch.RestoreCursorState();
            }
        }

        private static Font defaultFont;
        private static bool unlockCursor;

        public static Font DefaultFont
        {
            get
            {
                if (defaultFont != null) return defaultFont;
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return defaultFont;
            }
        }

        public static Color DefaultTextColor { get; } = new Color(0.95f, 0.95f, 0.95f, 1f);
        public static event Action Initialized;

        public static void Initialize()
        {
            ClassInjector.RegisterTypeInIl2Cpp<ActionRunner>();
            SceneManager.add_sceneLoaded((UnityAction<Scene, LoadSceneMode>)OnSceneLoad);
        }

        private static void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            if (CanvasRoot != null) return;
            CreateRootCanvas().transform.SetAsFirstSibling();
            Initialized?.Invoke();
        }

        public static GameObject CreateButton(string label,
            Transform parent,
            out Text text,
            Color? normalColor = null,
            Color? pressedColor = null,
            Color? highlightedColor = null,
            int fontSize = 15,
            Action callback = null)
        {
            var buttonGO = GOFactory.CreateObject("Button", parent,
                out Image img,
                out Button button);

            button.onClick.AddListener(callback);

            img.type = Image.Type.Sliced;
            img.color = new Color(1, 1, 1, 0.75f);

            var textGO = GOFactory.CreateObject("Text", buttonGO.transform,
                out RectTransform textTransform,
                out text);
            textGO.layer = LayerManager.LAYER_UI;

            SetDefaultColorTransitionValues(button);

            var colors = button.colors;
            if (normalColor != default) colors.normalColor = (Color)normalColor;
            if (pressedColor != default) colors.pressedColor = (Color)pressedColor;
            if (highlightedColor != default) colors.highlightedColor = (Color)highlightedColor;
            button.colors = colors;

            text.text = label;
            text.fontSize = fontSize;
            text.color = DefaultTextColor;
            text.font = DefaultFont;
            text.alignment = TextAnchor.MiddleCenter;

            textTransform.anchorMin = Vector2.zero;
            textTransform.anchorMax = Vector2.one;
            textTransform.sizeDelta = Vector2.zero;

            return buttonGO;
        }

        public static void SetDefaultColorTransitionValues(Selectable selectable)
        {
            var colors = selectable.colors;
            colors.normalColor = new Color(0.35f, 0.35f, 0.35f);
            colors.highlightedColor = new Color(0.45f, 0.45f, 0.45f);
            colors.pressedColor = new Color(0.25f, 0.25f, 0.25f);

            // fix to make all buttons become de-selected after being clicked
            // (ColorBlock.selectedColor is commonly stripped)
            if (selectable is Button button)
            {
                button.onClick.AddListener((UnityAction)Deselect);
                void Deselect() => button.OnDeselect(null);
            }

            selectable.colors = colors;
        }

        public static GameObject CreateViewport(Transform parent)
        {
            var go = GOFactory.CreateObject("Viewport", parent,
                out HorizontalLayoutGroup group);
            go.layer = LayerManager.LAYER_UI;

            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = true;
            group.childForceExpandWidth = false;

            return go;
        }

        public static GameObject CreateTitleBar(
            string title,
            Transform parent,
            out Text text,
            int fontSize = 15,
            int height = 15,
            Color? color = null)
        {
            var titleBarGO = GOFactory.CreateObject("TitleBar", parent,
                out HorizontalLayoutGroup titleGroup,
                out Image img,
                out LayoutElement titleLayout);
            titleBarGO.layer = LayerManager.LAYER_UI;

            titleGroup.childAlignment = TextAnchor.UpperLeft;
            titleGroup.childControlWidth = true;
            titleGroup.childControlHeight = true;
            titleGroup.childForceExpandHeight = true;
            titleGroup.childForceExpandWidth = true;
            titleLayout.minHeight = height;
            titleLayout.flexibleHeight = 0;
            img.color = color ?? new Color(44f / 255f, 44f / 255f, 44f / 255f);

            var label = GOFactory.CreateObject("Label", titleBarGO.transform,
                out text,
                out LayoutElement textLayout);
            label.layer = LayerManager.LAYER_UI;

            text.color = DefaultTextColor;
            text.font = DefaultFont;
            text.alignment = TextAnchor.MiddleCenter;
            text.supportRichText = true;
            text.text = title;
            text.fontSize = fontSize;
            textLayout.flexibleWidth = 5000;

            return titleBarGO;
        }

        public static GameObject CreatePanel(string name, Transform parent, out GameObject content, Color? color = null)
        {
            var panelGO = GOFactory.CreateObject(name, parent,
                out RectTransform panelTransform,
                out ContentSizeFitter panelFitter,
                out VerticalLayoutGroup panelGroup);
            panelGO.layer = LayerManager.LAYER_UI;

            panelTransform.anchorMin = Vector2.zero;
            panelTransform.anchorMax = Vector2.one;
            panelTransform.anchoredPosition = Vector2.zero;
            panelTransform.sizeDelta = Vector2.zero;

            panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            panelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            panelGroup.childControlHeight = true;
            panelGroup.childControlWidth = true;
            panelGroup.childForceExpandHeight = true;
            panelGroup.childForceExpandWidth = true;

            content = GOFactory.CreateObject("Content", panelGO.transform,
                out Image img,
                out VerticalLayoutGroup contentGroup);
            content.layer = LayerManager.LAYER_UI;

            img.type = Image.Type.Filled;
            img.color = color ?? new Color(0.1f, 0.1f, 0.1f);

            contentGroup.padding.left = 3;
            contentGroup.padding.right = 3;
            contentGroup.padding.bottom = 3;
            contentGroup.padding.top = 3;
            contentGroup.spacing = 3;
            contentGroup.childControlHeight = true;
            contentGroup.childControlWidth = true;
            contentGroup.childForceExpandHeight = false;
            contentGroup.childForceExpandWidth = true;

            return panelGO;
        }

        public static GameObject CreateRootCanvas()
        {
            var root = GOFactory.CreateObject($"{VersionInfo.RootNamespace}Canvas", null,
                out Canvas canvas,
                out CanvasScaler scaler,
                out ActionRunner _,
                out GraphicRaycaster _);
            root.layer = LayerManager.LAYER_UI;

            UnityEngine.Object.DontDestroyOnLoad(root);

            CanvasRoot = root;
            CanvasRoot.transform.position = new Vector3(0f, 0f, 1f);

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.referencePixelsPerUnit = 100;
            canvas.sortingOrder = 998;

            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;

            root.SetActive(true);

            return root;
        }
    }
}

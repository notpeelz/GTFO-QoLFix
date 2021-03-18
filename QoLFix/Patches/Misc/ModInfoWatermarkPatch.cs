using BepInEx.Configuration;
using TMPro;
using UnityEngine;

namespace QoLFix.Patches.Misc
{
    public class ModInfoWatermarkPatch : Patch
    {
        private const string PatchName = nameof(ModInfoWatermarkPatch);

#if RELEASE
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");
#endif

        public static Patch Instance { get; private set; }

        public override string Name { get; } = PatchName;

#if RELEASE
        public override bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;
#else
        public override bool Enabled => true;
#endif

        public override void Initialize()
        {
            Instance = this;
#if RELEASE
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Displays the build info (mod version) on the bottom right of the screen."));
#endif
        }

        public override void Execute()
        {
            this.PatchMethod<WatermarkGuiLayer>(nameof(WatermarkGuiLayer.Setup), new[] { typeof(Transform), typeof(string) }, PatchType.Postfix);
        }

        private static void WatermarkGuiLayer__Setup__Postfix(WatermarkGuiLayer __instance)
        {
            var watermark = __instance.m_watermark;

            var go = GOFactory.CreateObject("ModInfo", watermark.transform,
                    out RectTransform t,
                    out TextMeshPro text,
                    out Cell_TMProDisabler _);
            go.layer = LayerManager.LAYER_UI;

            var fpsText = watermark.m_fpsText;
            var watermarkTransform = watermark.m_watermarkText.GetComponent<RectTransform>();

            t.position = watermarkTransform.position;
            t.localPosition = watermarkTransform.localPosition;
            t.localScale = watermarkTransform.localScale;
            t.anchorMin = watermarkTransform.anchorMin;
            t.anchorMax = watermarkTransform.anchorMax;
            t.pivot = watermarkTransform.pivot;
            t.offsetMax = watermarkTransform.offsetMax;
            t.offsetMin = watermarkTransform.offsetMin;
            t.anchoredPosition = watermarkTransform.anchoredPosition;

            text.isOrthographic = fpsText.isOrthographic;
            text.font = fpsText.font;
            text.color = fpsText.color;
            text.alpha = fpsText.alpha;
            text.fontSize = fpsText.fontSize;
            text.fontSizeMin = fpsText.fontSizeMin;
            text.fontSizeMax = fpsText.fontSizeMax;
            text.fontWeight = fpsText.fontWeight;
            text.fontStyle = fpsText.fontStyle;
            text.enableKerning = fpsText.enableKerning;
            text.enableWordWrapping = false;
            text.alignment = fpsText.alignment;
            text.autoSizeTextContainer = fpsText.autoSizeTextContainer;
            text.UpdateMaterial();
            text.UpdateFontAsset();

            var versionText = $"QoLFix {VersionInfo.Version}";
            if (!string.IsNullOrEmpty(VersionInfo.VersionPrerelease))
            {
                versionText += $"-{VersionInfo.VersionPrerelease}";
            }

            if (QoLFixPlugin.IsReleaseBuild)
            {
#if RELEASE_STANDALONE
                versionText += " SE";
#elif RELEASE_THUNDERSTORE
                versionText += " TE";
#endif
            }

            if (VersionInfo.GitIsDirty || !QoLFixPlugin.IsReleaseBuild)
            {
                text.color = new Color(238 / 255f, 210 / 255f, 2 / 255f);
                versionText += " DEV";
            }

            text.SetText(versionText.ToUpper());
            text.ForceMeshUpdate(true);

            var offset = new Vector3(0, text.GetRenderedHeight(true), 0);
            watermark.transform.localPosition += offset;
            text.transform.localPosition -= offset;
        }
    }
}

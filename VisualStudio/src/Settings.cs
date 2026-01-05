using ModSettings;
using System.Runtime.CompilerServices;

namespace SCPlus
{
    internal static class Settings
    {
        public static UISprite hueSliderThumb;
        public static void OnLoad()
        {
            Settings.options = new SCPSettings();
            Settings.options.AddToModSettings("Safehouse Customization Plus");
            

        }

        public static void OnInitialize()
        {
            hueSliderThumb = InterfaceManager.GetPanel<Panel_OptionsMenu>().transform.Find("Pages/ModSettings/GameObject/ScrollPanel/Offset/Mod settings grid (Safehouse Customization Plus)/Custom Setting (Hue)/Slider_FOV/Slider_Options/Thumb").GetComponent<UISprite>();
            hueSliderThumb.color = outlineColor.HueAdjust(Settings.options.outlineHue);
            ShowDistance(Settings.options.outlineVisibility == 1);
            ShowOutline(Settings.options.outlineVisibility != 3);
        }

        internal static void ShowDistance(bool visible)
        {
            options.SetFieldVisible(nameof(options.outlineDistance), visible);
        }

        internal static void ShowOutline(bool visible)
        {
            options.SetFieldVisible(nameof(options.outlineHue), visible);
            options.SetFieldVisible(nameof(options.outlineAlpha), visible);
            options.SetFieldVisible(nameof(options.outlineThickness), visible);
        }

        public static SCPSettings options;
    }

    internal class SCPSettings : JsonModSettings
    {
        [Section("General")]
        /*
        [Name("Enable customization anywhere")]
        [Description("Keep in mind that it's not intended and can be wonky")]
        public bool enableCustomizationAnywhere = false;

        [Name("Pickup any container")]
        [Description("Containers that weren't meant to be picked up won't have inventory icons and can have 0 weight")]
        public bool pickupContainers = false;

        [Name("Pickup anything that moves")]
        [Description("Things won't have inventory icons and can have 0 weight")]
        public bool pickupAnything = false;
        */
        [Name("Hide house icon")]
        [Description("Hide safehouse customization icon\n\nDefault: Only hide when not customizing")]
        [Choice(new string[]
        {
            "Vanilla",
            "Only hide when not customizing",
            "Always hide"
        })]
        public int hideIcon = 1;

        [Name("Alternate workbench interaction")]
        [Description("Interact with workbenches like before. RMB to break down\n\nDefault: true")]
        public bool altWorkbenchInteraction = true;

        [Name("Weight approximation")]
        [Description("Simple attempt to calculate decoration weight from its material and model volume. Can produce funny results\n\nDefault: true")]
        public bool doWeightCalculation = true;

        [Name("Weight multiplier")]
        [Description("Adjust weight approximation to your liking (updated on newly acquired items or after game restart)\n\nDefault: 1")]
        [Slider(0.1f, 2f, 20)]
        public float autoWeightMultiplier = 1f;

        [Section("Outline")]

        [Name("Visibility")]
        [Description("When to show outlines\n\nRe-enter customization mod to properly apply")]
        [Choice(new string[]
        {
            "Vanilla",
            "When close to player", 
            "When looked at",
            "Disabled"
        })]
        public int outlineVisibility = 0;

        [Name("Visibility distance")]
        [Description("For distance based outline visibility. Lower values will make it feel like you bumping into stuff makes it glow")]
        [Slider(0f, 16f, 33)]
        public float outlineDistance = 3f;

        [Name("Hue")]
        [Description("Default: 0.61")]
        [Slider(0f, 1f, 101, NumberFormat = "{0:0.00}")]
        public float outlineHue = 0.61f;

        [Name("Alpha")]
        [Description("Default: 0.15")]
        [Slider(0f, 1f, 21, NumberFormat = "{0:0.00}")]
        public float outlineAlpha = 0.15f;

        [Name("Thickness")]
        [Description("Default: 4")]
        [Slider(0f, 20f, 21)]
        public float outlineThickness = 4f;

        [Section("Dev")]
        [Name("Enable developer inspect mode")]
        [Description("Sprint + RMB on any item to inpect, arrows to adjust position, +/- to zoom, 0 to take screenshot with object name")]
        public bool devInspect = false;

        [Name("Debug")]
        [Description("Send debug info to console")]
        public bool debugLog = false;

        [Name("Load interval")]
        [Description("Skip frame after how many objects checked. Lower values will lag less but take longer, higher - lag more and take less time\n\nDO NOT quit or enter transition while it loads")]
        [Slider(2, 100)]
        public int carryableProcessingInterval = 10;


        [Section("Cheats")]
        [Name("Duplicate")]
        [Description("Duplicate decoration item under your crosshair")]
        public KeyCode dupeKey = KeyCode.None;

        [Name("Ignore weight when placing")]
        [Description("Ignore decoration weight while placing")]
        public bool ignorePlaceWeight = false;

        [Name("Global weight modifier")]
        [Description("Updated on newly acquired items or after game restart")]
        [Slider(0f, 1f, 11)]
        public float globalWeightModifier = 1f;

        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
            if (field.Name == nameof(outlineHue))
            {
                Settings.hueSliderThumb.color = outlineColor.HueAdjust((float)newValue);
            }

            if (field.Name == nameof(outlineVisibility))
            {
                Settings.ShowDistance((int)newValue == 1);
                Settings.ShowOutline((int)newValue != 3);
            }
        }

        protected override void OnConfirm()
        {
            base.OnConfirm();

            SafehouseManager sm = GameManager.GetSafehouseManager();

            sm.m_OutlineColor = outlineColor.HueAdjust(Settings.options.outlineHue).AlphaAdjust(Settings.options.outlineAlpha);
            sm.m_OnHoverColor = outlineColor.HueAdjust(Settings.options.outlineHue);

            sm.m_OnHoverPropertyBlock.SetColor("_Color", sm.m_OnHoverColor);

            sm.m_OutlineThickness = Settings.options.outlineThickness;

            sm.DisableOutlineRendering();
            sm.EnableOutlineRendering();
            

            if (GameManager.GetSafehouseManager().IsCustomizing())
            {
                //sm.StartCustomizing();

                if (Settings.options.outlineVisibility == 1)
                {
                    SCPlusDecorationDetector comp = GameManager.GetPlayerTransform().gameObject.GetOrAddComponent<SCPlusDecorationDetector>();
                    if (comp.cc != null) comp.cc.radius = Settings.options.outlineDistance;
                }
                else
                {
                    if (GameManager.GetPlayerTransform().TryGetComponent(out SCPlusDecorationDetector detector))
                    {
                        GameObject.Destroy(detector);
                    }
                }
            }
        }
    }
}

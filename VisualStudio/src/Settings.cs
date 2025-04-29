using ModSettings;

namespace SCPlus
{
    internal static class Settings
    {
        public static void OnLoad()
        {
            Settings.options = new SCPSettings();
            Settings.options.AddToModSettings("Safehouse Customization Plus");
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
        [Description("Adjust weight approximation to your liking\n\nDefault: 1")]
        [Slider(0.1f, 2f, 20)]
        public float autoWeightMultiplier = 1f;

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

        [Name("Ignore weight")]
        [Description("Ignore decoration weight while placing")]
        public bool ignorePlaceWeight = false;

        protected override void OnConfirm()
        {
            base.OnConfirm();

        }
    }
}

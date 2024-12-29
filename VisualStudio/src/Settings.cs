using Il2CppTLD.AddressableAssets;
using ModSettings;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;

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
        [Description("...")]
        [Choice(new string[]
        {
            "Vanilla",
            "Only hide when not customizing",
            "Always hide"
        })]
        public int hideIcon;

        [Name("Alternate workbench interaction")]
        [Description("Interact with workbenches like before. RMB to break down")]
        public bool altWorkbenchInteraction = false;

        [Section("Dev")]
        [Name("Enable developer inspect mode")]
        [Description("Sprint + RMB on any item to inpect, arrows to adjust position, +/- to zoom, 0 to take screenshot with object name")]
        public bool devInspect = false;
        
        [Name("Debug")]
        [Description("Send debug info to console")]
        public bool debugLog = false;


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

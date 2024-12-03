using ModSettings;

namespace BCP
{
    internal static class Settings
    {
        public static void OnLoad()
        {
            Settings.options = new BCPSettings();
            Settings.options.AddToModSettings("Safehouse Customization Plus");
        }

        public static BCPSettings options;
    }

    internal class BCPSettings : JsonModSettings
    {
        [Section("General")]

        [Name("Enable customization anywhere")]
        [Description("Keep in mind that it's not intended and can be wonky")]
        public bool enableCustomizationAnywhere = false;


        [Name("Pickup any container")]
        [Description("Containers that weren't meant to be picked up won't have inventory icons and can have 0 weight")]
        public bool pickupContainers = false;

        [Name("Pickup anything that moves")]
        [Description("Things won't have inventory icons and can have 0 weight")]
        public bool pickupAnything = false;


        protected override void OnConfirm()
        {
            base.OnConfirm();
        }
    }
}

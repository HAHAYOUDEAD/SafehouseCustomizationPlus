global using static BCP.Utility;

namespace BCP
{
    public class BCPMain : MelonMod
    {
        public bool isLoaded = false;

        public static string modsPath;

        public override void OnInitializeMelon()
        {
            modsPath = Path.GetFullPath(typeof(MelonMod).Assembly.Location + "/../../../Mods/");

            Settings.OnLoad();
        }
    }
}





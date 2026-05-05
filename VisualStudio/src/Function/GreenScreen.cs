namespace SCPlus
{
    internal class GreenScreen
    {

        public static void SetupGreenScreen(Camera cam, bool reset = false)
        {
            if (reset)
            {
                GameManager.GetMainCamera().cullingMask = 490708959;
                GameManager.GetMainCamera().clearFlags = CameraClearFlags.Skybox;
            }
            else
            {
                GameManager.GetMainCamera().cullingMask = 0;
                GameManager.GetMainCamera().clearFlags = CameraClearFlags.SolidColor;
                GameManager.GetMainCamera().backgroundColor = new Color(0.15f, 0.6f, 0.25f, 1f);
            }
        }

        public static string TakeScreenshot()
        {
            DecorationItem di = GameManager.GetPlayerManagerComponent().GearItemBeingInspected().transform.GetComponentInChildren<DecorationItem>();
            string s = "Couldn't get object name, took screenshot anyways";
            string sanitizedName = "";
            if (di)
            {
                sanitizedName = SanitizeObjectName(di.name);
            }
            string path = Directory.CreateDirectory(modsPath + modFolder + "Screenshots/").FullName;
            if (sanitizedName == "")
            {
                sanitizedName = Il2Cpp.Utils.GetGuid();
            }
            if (SCPMain.catalogParsed.ContainsKey(sanitizedName) || (di.IconReference.RuntimeKeyIsValid() && di.IconReference.RuntimeKey.ToString() != SCPMain.catalogParsed[placeholderIconName]))
            {
                path += "!";
                s = "Duplicate screenshot for " + sanitizedName;
            }

            path += sanitizedName;
            path += ".png";

            if (File.Exists(path))
            {
                s = "Overwritten screenshot for " + sanitizedName;
            }
            else
            {
                s = "Took screenshot for " + sanitizedName;
            }

            ScreenCapture.CaptureScreenshot(path);

            MelonCoroutines.Start(DelayedHUDMessage(s, 0.5f));
            return path;
        }
    }
}

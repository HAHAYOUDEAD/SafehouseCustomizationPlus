global using static SCPlus.Utility;
using Il2Cpp;
using Il2CppTLD.AddressableAssets;
using Il2CppTLD.IntBackedUnit;
using Il2CppTLD.Trader;
using Il2CppVLB;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine.AddressableAssets;

namespace SCPlus
{
    public class SCPMain : MelonMod
    {
        public static bool isLoaded = false;

        public static string modsPath;

        public static bool DEVInspectMode;
        public static float DEVInspectModeMoveStep = 0.01f;
        public static GameObject DEVInspectTempGO;

        public static Dictionary<string, string> fireBarrelData = new (); // WoodStove guid | serialized fire
        public static Dictionary<string, float> objectWeightOverride = new (); // object name | weight

        public static bool justDupedContainer;

        public override void OnInitializeMelon()
        {
            modsPath = Path.GetFullPath(typeof(MelonMod).Assembly.Location + "/../../../Mods/");
            LocalizationManager.LoadJsonLocalization(LoadEmbeddedJSON("Localization.json"));
            Settings.OnLoad();
            MelonCoroutines.Start(ConsoleCommands.CONSOLE_PopulateDecortionsListEnum());
            //Il2CppTLD.AddressableAssets.AssetHelper.SafeLoadAssetAsync<GameObject>("Assets/ArtAssets/Env/Objects/OBJ_IndustrialDeco/OBJ_PlasticBarrelA_Prefab.prefab").WaitForCompletion().GetOrAddComponent<ObjectGuid>().MaybeRuntimeRegister();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (IsScenePlayable()) isLoaded = true;
            DecorativePatches.injectPdids = false;
        }
        
        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            DecorativePatches.injectPdids = true;
        }

        public static void SetupGreenscreen(Camera cam, bool reset = false)
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
                sanitizedName = di.name.Replace("(PLACED)", "").Replace("(Clone)", "").Replace(" ", "");
            }
            string path = Directory.CreateDirectory(modsPath + "/SCPlus/Screenshots").FullName;
            if (sanitizedName == "")
            {
                sanitizedName = Il2Cpp.Utils.GetGuid();
            }
            path += "/" + sanitizedName;
            path += ".png";

            ScreenCapture.CaptureScreenshot(path);
            if (File.Exists(path))
            {
                s = "Overwritten screenshot for " + sanitizedName;
            }
            else
            {
                s = "Took screenshot for " + sanitizedName;
            }
            
            MelonCoroutines.Start(DelayedHUDMessage(s, 0.5f));
            return path;
        }

        public static IEnumerator DelayedHUDMessage(string text, float delay)
        {
            float n = 0f;

            while (n <= delay)
            { 
                n += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            HUDMessage.AddMessage(text, false, true);
            yield break;
        }

        public static void SetupDecorationItem(DecorationItem di)
        {
            if (!di) return;
            /* somewhat handled by HL
            float weight;
            float baseWeight = 2f;
            switch (di.tag)
            {
                case "Wood":
                    weight = baseWeight * 0.5f;
                    break;
                case "Metal":
                    weight = baseWeight * 3f;
                    break;
                case "Rug":
                    weight = baseWeight * 0.25f;
                    break;
                default:
                    weight = baseWeight;
                    break;
            }
            if (weight == baseWeight)
            {
                if (di.name.ToLower().Contains("wood")) weight *= 0.5f;
                if (di.name.ToLower().Contains("metal")) weight *= 3f;
                //if (di.name.ToLower().Contains("wood")) weight *= 0.5f;
            }
            float volume = di.GetComponentInChildren<Collider>().bounds.GetVolumeCubic();
            //MelonLogger.Msg($"{di.name} volume {volume} weight {volume * weight}");
            di.m_Weight = ItemWeight.FromKilograms(Mathf.Clamp(Mathf.Round(volume * weight * 100f / 25f) * 25f / 100f, 0.1f, 50f)); //round to 0.25
            */

        }
        
        public override void OnUpdate()
        {

            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.options.dupeKey))
            {
                if (!GameManager.GetSafehouseManager().IsCustomizing()) return;

                GameObject go = GetInteractiveGameObjectUnderCrosshair();
                DecorationItem di = go?.GetComponent<DecorationItem>();
                if (di)
                {
                    GameObject dupe = GameObject.Instantiate(go);
                    dupe.name = SanitizeObjectName(dupe.name);
                    GameManager.GetPlayerManagerComponent().StartPlaceMesh(dupe, PlaceMeshFlags.DestroyOnCancel, di.m_PlacementRules);
                    justDupedContainer = true;


                }
            }   
            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.Insert))
            {
                if (InterfaceManager.DetermineIfOverlayIsActive()) return;

                GameObject go = GetRealGameObjectUnderCrosshair();
                if (go && !go.GetComponentInChildren<DecorationItem>())
                {
                    if (go.GetComponent<TraderRadio>())
                    {
                        go.GetComponent<TraderRadio>().enabled = false;
                        go.GetComponent<BlockPlacement>().m_BlockDecorationItemPlacement = false;
                    }
                    
                    if (go.GetComponent<BreakDown>())
                    {
                        go.GetComponent<BreakDown>().m_AllowEditModePlacement = true;
                    }

                    if (go.GetComponent<WaterSource>())
                    {
                        // Toilet
                    }

                    if (go.GetComponent<MillingMachine>())
                    {
                        // MillingMachine
                    }
                    if (go.GetComponent<AmmoWorkBench>())
                    {
                        // AmmoWorkBench
                    }



                    LocalizedString ls = TryGetLocalizedName(go);
                    DecorationItem di = go.AddComponent<DecorationItem>();
                    go.layer = vp_Layer.InteractiveProp;
                    
                    //AssetReferenceDecorationItem ardi = new AssetReferenceDecorationItem("INTERACTIVE_LimbA_Prefab");
                    di.m_DecorationPrefab = new AssetReferenceDecorationItem(SanitizeObjectName(go.name));
                    di.GetDecorationPrefab();
                    di.m_DisplayName = ls;//bd ? bd.m_LocalizedDisplayName : new LocalizedString() { m_LocalizationID = "NaN" };
                    di.GetCraftingDisplayName();
                    di.m_IconReference = new AssetReferenceTexture2D("");
                    di.GetInventoryIconTexture();
                    di.Awake();
                    //Placeable pl = go.GetOrAddComponent<Placeable>();
                    //pl.m_Addressable = new AssetReferencePlaceable(pl.m_Guid);
                    //PlaceableManager.Add(pl);
                    // trader radio



                    HUDMessage.AddMessage(Localization.Get("SCP_Action_AttemptToMakeMovable") + TryGetLocalizedName(go).Text(), false, true);
                }
                //RadialSpawnManager.GetPrefabFromName("INTERACTIVE_LimbA_Prefab");
                //AssetHelper.SafeLoadAssetAsync<GameObject>("INTERACTIVE_LimbA_Prefab").WaitForCompletion();
                //AssetHelper.ValidateKey<GameObject>("INTERACTIVE_LimbA_Prefab");
                //new AssetReference("INTERACTIVE_LimbA_Prefab").RuntimeKey.ToString);
            }



            if (!DEVInspectMode) return;

            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.UpArrow))
            {
                if (GameManager.GetPlayerManagerComponent().IsInspectModeActive())
                {
                    if (InputManager.GetSprintDown(InputManager.m_CurrentContext)) DEVInspectModeMoveStep = 0.002f;
                    Transform t = GameManager.GetPlayerManagerComponent().GearItemBeingInspected().transform;
                    t.transform.localPosition += Vector3.up * DEVInspectModeMoveStep;
                }
            }

            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.DownArrow))
            {
                if (GameManager.GetPlayerManagerComponent().IsInspectModeActive())
                {
                    if (InputManager.GetSprintDown(InputManager.m_CurrentContext)) DEVInspectModeMoveStep = 0.002f;
                    Transform t = GameManager.GetPlayerManagerComponent().GearItemBeingInspected().transform;
                    t.transform.localPosition += Vector3.down * DEVInspectModeMoveStep;
                }
            }

            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.LeftArrow))
            {
                if (GameManager.GetPlayerManagerComponent().IsInspectModeActive())
                {
                    if (InputManager.GetSprintDown(InputManager.m_CurrentContext)) DEVInspectModeMoveStep = 0.002f;
                    Transform t = GameManager.GetPlayerManagerComponent().GearItemBeingInspected().transform;
                    t.transform.localPosition += Vector3.left * DEVInspectModeMoveStep;
                }
            }

            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.RightArrow))
            {
                if (GameManager.GetPlayerManagerComponent().IsInspectModeActive())
                {
                    if (InputManager.GetSprintDown(InputManager.m_CurrentContext)) DEVInspectModeMoveStep = 0.002f;
                    Transform t = GameManager.GetPlayerManagerComponent().GearItemBeingInspected().transform;
                    t.transform.localPosition += Vector3.right * DEVInspectModeMoveStep;
                }
            }

            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.Equals))
            {
                if (GameManager.GetPlayerManagerComponent().IsInspectModeActive())
                {
                    if (InputManager.GetSprintDown(InputManager.m_CurrentContext)) DEVInspectModeMoveStep = 0.002f;
                    Transform t = GameManager.GetPlayerManagerComponent().GearItemBeingInspected().transform;
                    t.transform.localScale += Vector3.one * DEVInspectModeMoveStep * 5f;
                }
            }

            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.Minus))
            {
                if (GameManager.GetPlayerManagerComponent().IsInspectModeActive())
                {
                    if (InputManager.GetSprintDown(InputManager.m_CurrentContext)) DEVInspectModeMoveStep = 0.002f;
                    Transform t = GameManager.GetPlayerManagerComponent().GearItemBeingInspected().transform;
                    t.transform.localScale -= Vector3.one * DEVInspectModeMoveStep * 5f;
                }
            }

            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.Comma))
            {
                if (GameManager.GetPlayerManagerComponent().IsInspectModeActive())
                {
                    if (InputManager.GetSprintDown(InputManager.m_CurrentContext)) DEVInspectModeMoveStep = 0.06f;
                    Transform t = GameManager.GetInspectModeLight().transform;
                    t.transform.localEulerAngles += Vector3.up * DEVInspectModeMoveStep * 200f;
                }
            }

            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.Period))
            {
                if (GameManager.GetPlayerManagerComponent().IsInspectModeActive())
                {
                    if (InputManager.GetSprintDown(InputManager.m_CurrentContext)) DEVInspectModeMoveStep = 0.06f;
                    Transform t = GameManager.GetInspectModeLight().transform;
                    t.transform.localEulerAngles += Vector3.down * DEVInspectModeMoveStep * 200f;
                }
            }

            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.Alpha0))
            {
                if (GameManager.GetPlayerManagerComponent().IsInspectModeActive())
                {
                    TakeScreenshot();
                }
            }
        }
    }
}





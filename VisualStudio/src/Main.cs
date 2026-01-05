using Il2CppSystem.Net;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Diagnostics;
using Il2CppSteamworks;
using Il2CppTLD.OptionalContent;
using UnityEngine.ResourceManagement.ResourceProviders;
using Il2CppTLD.BigCarry;

namespace SCPlus
{
    public class SCPMain : MelonMod
    {
        public static bool isLoaded = false;
        public static bool hasTFTFTF;

        public static string modsPath;

        public static bool DEVInspectMode;
        public static float DEVInspectModeMoveStep = 0.01f;
        public static GameObject DEVInspectTempGO;

        public static IResourceLocator catalogLocator;
        public static Dictionary<string, string> catalogParsed = new();
        public static HashSet<string> iconGuidLookupList = new();
        
        public static Dictionary<string, float> autoWeightTable = new(); // object name | weight

        public static bool instantiatingCarryables;
        public static bool decorationJustDuped;

        public static bool decorationListPopulated;

        public static int carryableCoroutineCounter = 0;

        public override void OnInitializeMelon()
        {

            modsPath = Path.GetFullPath(typeof(MelonMod).Assembly.Location + "/../../../Mods/");
            LocalizationManager.LoadJsonLocalization(ResourceHandler.LoadEmbeddedJSON("Localization.json"));

            Settings.OnLoad();

            ResourceHandler.ExtractFolderFromResources(modsPath + modFolder, resourcesFolder + resourcesFolderForAssumingPlayersUnableToRead, true);

            AsyncOperationHandle<IResourceLocator> handle = null;
            try
            {
                string path = modsPath + iconsFolder + iconsCatalog + ".json";
                handle = Addressables.LoadContentCatalogAsync(path);
                catalogLocator = handle.WaitForCompletion();
                if (catalogLocator != null && catalogLocator.Keys != null)
                {
                    for (int i = 0; i < catalogLocator.Keys.ToList().Count; i++)
                    {
                        string thisString = catalogLocator.Keys.ElementAt(i).ToString();
                        if (Guid.TryParse(thisString, out _))
                        {
                            string prevString = catalogLocator.Keys.ElementAt(i - 1).ToString().Replace("ico__", "");
                            catalogParsed[prevString] = thisString;
                            iconGuidLookupList.Add(thisString);
                        }
                    }
                }
                else
                {
                    Log(CC.Red, $"Catalog {iconsCatalog} could not be found at {path}");
                }
            }
            catch (Exception e)
            {
                Log(CC.Red, $"Catalog {iconsCatalog} load failed: " + e.ToString());
            }
            
            //handle?.Release(); 
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (IsScenePlayable()) isLoaded = true;
            DecorationPatches.injectPdids = false;
            if (IsMainMenu(sceneName))
            {
                hasTFTFTF = OptionalContentManager.Instance.InstalledContent.ContainsKey("2091330");
                Settings.OnInitialize();
            }
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (!IsScenePlayable()) return;

            Scene currentScene = SceneManager.GetSceneByName(sceneName);

            if (sceneName.EndsWith("_SANDBOX"))
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                foreach (var found in FindObjectsOfTypeInScene<WoodStove>(currentScene))
                {
                    if (TryGetCarryableRoot(found.transform, out GameObject? root))
                    {
                        SCPMain.MakeIntoDecoration(root);
                    }
                }
                foreach (var found in FindObjectsOfTypeInScene<MillingMachine>(currentScene))
                {
                    if (TryGetCarryableRoot(found.transform, out GameObject? root))
                    {
                        SCPMain.MakeIntoDecoration(root);
                    }
                }

                foreach (var found in FindObjectsOfTypeInScene<AmmoWorkBench>(currentScene))
                {
                    if (TryGetCarryableRoot(found.transform, out GameObject? root))
                    {
                        SCPMain.MakeIntoDecoration(root);
                    }
                }

                foreach (var found in FindObjectsOfTypeInScene<ObjectAnim>(currentScene)) // containers
                {
                    if (TryGetCarryableRoot(found.transform, out GameObject? root))
                    {
                        SCPMain.MakeIntoDecoration(root);
                    }
                }
                stopwatch.Stop();
                Log(CC.Blue, $"SC+ Lookup pass 2: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedTicks} ticks)");
            }
            else if (sceneName.EndsWith("_DLC01"))
            {
                foreach (var found in FindObjectsOfTypeInScene<TraderRadio>(currentScene))
                {
                    if (TryGetCarryableRoot(found.transform, out GameObject? root))
                    {
                        SCPMain.MakeIntoDecoration(root);
                    }
                }
            }
            else if (!sceneName.Contains("_"))
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                foreach (var found in FindObjectsOfTypeInScene<WoodStove>(currentScene))
                {
                    if (TryGetCarryableRoot(found.transform, out GameObject? root))
                    {
                        SCPMain.MakeIntoDecoration(root);
                    }
                }

                stopwatch.Stop();
                Log(CC.Blue, $"SC+ Lookup pass 1: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedTicks} ticks)");
            }
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            DecorationPatches.injectPdids = true;
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
                sanitizedName = SanitizeObjectName(di.name);
            }
            string path = Directory.CreateDirectory(modsPath + modFolder + "Screenshots/").FullName;
            if (sanitizedName == "")
            {
                sanitizedName = Il2Cpp.Utils.GetGuid();
            }
            if (SCPMain.catalogParsed.ContainsKey(sanitizedName) || (di.IconReference.RuntimeKeyIsValid() && di.IconReference.RuntimeKey.ToString() != catalogParsed[placeholderIconName]))
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

        public static void RelevantSetupForDecorationItem(DecorationItem di, bool weightOnly = false) // icons and weight
        {
            if (!di) return;
        
            float weight = 1f;
            float baseWeight = 2f;
            float volume = 1f;
            string name = SanitizeObjectName(di.name);
            bool change = true;

            if (CarryableData.decorationOverrideData.ContainsKey(name) && CarryableData.decorationOverrideData[name].weight != 0f)
            {
                weight = CarryableData.decorationOverrideData[name].weight * Settings.options.globalWeightModifier;
            }
            else if (di.name.ToLower().StartsWith("obj_curtain"))
            {
                weight = 0.5f;
            }
            else if (autoWeightTable.ContainsKey(name))
            {
                weight = autoWeightTable[name];
            }
            else if (Settings.options.doWeightCalculation)
            {
                switch (di.tag)
                {
                    case "Wood":
                        weight = baseWeight * 0.66f;
                        break;
                    case "Metal":
                        weight = baseWeight * 2.0f;
                        if (di.name.ToLower().Contains("barrel")) weight *= 0.5f;
                        break;
                    case "Rug":
                        weight = baseWeight * 0.25f;
                        break;
                    case "Glass":
                        weight = baseWeight * 0.5f;
                        break;
                    default:
                        weight = baseWeight;
                        break;
                }
                if (weight == baseWeight)
                {
                    if (di.name.ToLower().Contains("wood")) weight *= 0.66f;
                    if (di.name.ToLower().Contains("metal")) weight *= 2.0f;
                    if (di.name.ToLower().Contains("sack")) weight *= 0.25f;
                    if (di.name.ToLower().Contains("computer")) weight *= 0.5f;
                    if (di.name.ToLower().Contains("lamp")) weight *= 0.25f;
                }
                foreach (Collider c in di.GetComponentsInChildren<Collider>())
                {
                    volume += c.bounds.GetVolumeCubic();
                }

                weight = Mathf.Round(volume * weight * 100f / 25f) * 25f / 100f; //round to 0.25
                weight = Mathf.Clamp(weight, 0.1f, 30f);
                weight *= Settings.options.autoWeightMultiplier;
                weight *= Settings.options.globalWeightModifier;

                Log(CC.Blue, $"Approx. for {name}: weight: {weight}, volume: {volume}");

                autoWeightTable.Add(name, weight);
            }
            else 
            { 
                change = false;
            }

            if (change) di.m_Weight = ItemWeight.FromKilograms(weight); 

            if (weightOnly) return;

            if (catalogParsed.ContainsKey(name))
            {
                di.m_IconReference = new(catalogParsed[name]);
                if (!di.m_IconReference.RuntimeKeyIsValid())
                {
                    Log(CC.Red, $"Inconsistent icon GUID for decoration: {name}. Reverting to placeholder");
                    di.m_IconReference = new(catalogParsed[placeholderIconName]);
                }
            }
            else
            {
                di.m_IconReference = new(catalogParsed[placeholderIconName]);
                Log(CC.Magenta, $"Missing icon for decoration: {name}");
            }
        }

        public static void SetLayersToInteractiveProp(DecorationItem di)
        {
            foreach (string name in CarryableData.skipLayerChange)
            {
                if (di.name.ToLower().StartsWith(name.ToLower())) return;
            }

            foreach (Collider c in di.GetComponentsInChildren<Collider>())
            {
                if (c.gameObject.layer == vp_Layer.Default || c.gameObject.layer == vp_Layer.TerrainObject)
                {
                    
                    c.gameObject.layer = vp_Layer.InteractiveProp;
                }
            }
            if (di.gameObject.layer == vp_Layer.Default || di.gameObject.layer == vp_Layer.TerrainObject)
            {
                di.gameObject.layer = vp_Layer.InteractiveProp;
            }
        }

        public static void DisableNormalInteraction(DecorationItem di)
        {
            //di.enabled = true;

            if (di.GetComponent<TraderRadio>())
            {
                di.GetComponent<TraderRadio>().enabled = false;
                di.GetComponent<BlockPlacement>().m_BlockDecorationItemPlacement = false;
            }

            if (di.GetComponent<BreakDown>())
            {
                di.GetComponent<BreakDown>().m_AllowEditModePlacement = true;
            }
            if (di.name.ToLower().Contains("forge"))
            {
                di.gameObject.layer = vp_Layer.InteractiveProp;
            }
            if (di.GetComponent<WoodStove>())
            {
                di.GetComponent<WoodStove>().enabled = false;
            }
        }

        public static void RestoreNormalInteraction(DecorationItem di)
        {
            //di.enabled = false;

            if (di.GetComponent<TraderRadio>())
            {
                di.GetComponent<TraderRadio>().enabled = true;
                //go.GetComponent<BlockPlacement>().m_BlockDecorationItemPlacement = true;
            }
            if (di.name.ToLower().Contains("forge"))
            {
                di.gameObject.layer = vp_Layer.TerrainObject;
            }
            if (di.GetComponent<WoodStove>())
            {
                di.GetComponent<WoodStove>().enabled = true;
            }
        }
  

        public static DecorationItem? MakeIntoDecoration(GameObject go)
        {
            if (go && !go.GetComponentInChildren<DecorationItem>())
            {
                string name = SanitizeObjectName(go.name);

                LocalizedString ls = TryGetLocalizedName(go);
                DecorationItem di = go.AddComponent<DecorationItem>();
                SetLayersToInteractiveProp(di);

                di.m_DecorationPrefab = new(name);
                di.GetDecorationPrefab();
                di.m_DisplayName = ls;//bd ? bd.m_LocalizedDisplayName : new LocalizedString() { m_LocalizationID = "NaN" };
                di.GetCraftingDisplayName();
                if (name.ToLower().Contains("container"))
                {
                    di.m_PlacementRules = boxPlacementRules;
                }
                else
                {
                    di.m_PlacementRules = genericPlacementRules;
                }
                
                //di.m_IconReference = new("");

                RelevantSetupForDecorationItem(di);
                //MelonLogger.Msg(CC.Blue, $"Decoration item {di.name} created with icon {di.m_IconReference.RuntimeKey.ToString()}");

                if (!GameManager.GetSafehouseManager().IsCustomizing()) RestoreNormalInteraction(di);

                return di;
            }

            return go.GetComponentInChildren<DecorationItem>(); // can be null
        }

        public override void OnUpdate()
        {

            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.U))
            {
                if (Settings.options.debugLog && InterfaceManager.GetPanel<Panel_Inventory>().isActiveAndEnabled)
                {
                    DecorationItem di = InterfaceManager.GetPanel<Panel_Inventory>().GetCurrentlySelectedItem().m_DecorationItem;
                    if (di)
                    {
                        MelonLogger.Msg(CC.Blue, $"{{\"{SanitizeObjectName(di.name)}\", new() {{ weight = {di.Weight.ToQuantity(1f).ToString().Replace(',', '.')}f }} }},");
                        HUDMessage.AddMessage("Check console", false, true);
                    }
                    else
                    { 
                        HUDMessage.AddMessage("This is not a decoration item...", false, true);
                    }
                    
                }
            }

            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.options.dupeKey))
            {
                if (!GameManager.GetSafehouseManager().IsCustomizing()) return;

                GameObject go = GetInteractiveGameObjectUnderCrosshair();
                DecorationItem? di = go?.GetComponent<DecorationItem>();
                if (di != null)
                {
                    string name = SanitizeObjectName(di.name);

                    if (CarryableData.carryablePrefabDefinition.ContainsKey(name) && CarryableData.carryablePrefabDefinition[name].pickupable == false)
                    {
                        GameAudioManager.PlayGUIError();
                        HUDMessage.AddMessage(Localization.Get("SCP_Action_CantDuplicate"), false, true);
                        return;
                    }
                    GameObject dupe = GameObject.Instantiate(go);
                    dupe.name = name;
                    decorationJustDuped = true;
                    GameManager.GetPlayerManagerComponent().StartPlaceMesh(dupe, PlaceMeshFlags.DestroyOnCancel, di.m_PlacementRules);
                    
                }
            }
            /*
            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.Insert))
            {
                if (InterfaceManager.DetermineIfOverlayIsActive()) return;

                GameObject go = GetRealGameObjectUnderCrosshair();

                MakeIntoDecoration(go);
                //RadialSpawnManager.GetPrefabFromName("INTERACTIVE_LimbA_Prefab");
                //AssetHelper.SafeLoadAssetAsync<GameObject>("INTERACTIVE_LimbA_Prefab").WaitForCompletion();
                //AssetHelper.ValidateKey<GameObject>("INTERACTIVE_LimbA_Prefab");
                //new AssetReference("INTERACTIVE_LimbA_Prefab").RuntimeKey.ToString);
                HUDMessage.AddMessage(Localization.Get("SCP_Action_AttemptToMakeMovable") + TryGetLocalizedName(go).Text(), false, true);
            }
            */

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
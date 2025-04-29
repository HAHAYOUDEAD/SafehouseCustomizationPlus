using Il2CppSystem.Net;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Diagnostics;
using Il2CppSteamworks;

namespace SCPlus
{
    public class SCPMain : MelonMod
    {
        public static bool isLoaded = false;

        public static string modsPath;

        public static bool DEVInspectMode;
        public static float DEVInspectModeMoveStep = 0.01f;
        public static GameObject DEVInspectTempGO;

        public static IResourceLocator catalogLocator;
        public static Dictionary<string, string> catalogParsed = new();
        public static HashSet<string> iconGuidLookupList = new();
        
        public static Dictionary<string, float> autoWeightTable = new(); // object name | weight

        public static bool instantiatingCarryables;
        public static bool justDupedContainer;

        public static bool decorationListPopulated;

        public static int carryableCoroutineCounter = 0;

        public override void OnEarlyInitializeMelon()
        {
            base.OnEarlyInitializeMelon();
        }
        public override void OnInitializeMelon()
        {

            modsPath = Path.GetFullPath(typeof(MelonMod).Assembly.Location + "/../../../Mods/");
            LocalizationManager.LoadJsonLocalization(LoadEmbeddedJSON("Localization.json"));

            Settings.OnLoad();

            /*

            AsyncOperationHandle<IResourceLocator> handle = null;
            try
            {
                //UnityEngine.AddressableAssets.Addressables.LoadContentCatalogAsync("E:/SteamLibrary/steamapps/common/TheLongDark/Mods/SCPlus/Tex/catalog_TexSwapTest.json");

                //Il2CppTLD.AddressableAssets.AssetHelper.SafeLoadAssetAsync<Texture2D>("Assets/ArtAssets/Textures/Global/TRN_Snow_Ground_A_Noise.tga").WaitForCompletion();

                handle = UnityEngine.AddressableAssets.Addressables.LoadContentCatalogAsync(modsPath + "SCPlus/Tex/catalog_TexSwapTest.json");
                catalogLocator = handle.WaitForCompletion();
                if (catalogLocator != null && catalogLocator.Keys != null)
                {
                    for (int i = 0; i < catalogLocator.Keys.ToList().Count; i++)
                    {
                        MelonLogger.Msg(catalogLocator.Keys.ElementAt(i).ToString());
                    }
                }
                else
                {
                    Log(CC.Red, $"Catalog {iconsCatalog} could not be found at {modsPath}SCPlus/Tex/catalog_TexSwapTest.json");
                }
            }
            catch (Exception e)
            {
                Log(CC.Red, $"Catalog {iconsCatalog} load failed: " + e.ToString());
            }
            */

            AsyncOperationHandle<IResourceLocator> handle2 = null;
            try
            {
                string path = modsPath + iconsFolder + iconsCatalog + ".json";
                handle2 = Addressables.LoadContentCatalogAsync(path);
                catalogLocator = handle2.WaitForCompletion();
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
            string path = Directory.CreateDirectory(modsPath + "/SCPlus/Screenshots/").FullName;
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
                weight = CarryableData.decorationOverrideData[name].weight;
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
            if (di.name.ToLower().Contains("forge")) return;

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
            /*
            if (di.GetComponent<WaterSource>())
            {
                // Toilet
            }

            if (di.GetComponent<MillingMachine>())
            {
                // MillingMachine
            }
            if (di.GetComponent<AmmoWorkBench>())
            {
                // AmmoWorkBench
            }
            */
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
            /*
            if (di.GetComponent<BreakDown>())
            {
                di.GetComponent<BreakDown>().m_AllowEditModePlacement = true;
            }

            if (di.GetComponent<WaterSource>())
            {
                // Toilet
            }

            if (di.GetComponent<MillingMachine>())
            {
                // MillingMachine
            }
            if (di.GetComponent<AmmoWorkBench>())
            {
                // AmmoWorkBench
            }
            */
            if (di.name.ToLower().Contains("forge"))
            {
                di.gameObject.layer = vp_Layer.TerrainObject;
            }
            if (di.GetComponent<WoodStove>())
            {
                di.GetComponent<WoodStove>().enabled = true;
            }
        }

        internal static IEnumerator ProcessCarryables(List<CarryableSaveDataProxy> dataList, int cutoff)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Stopwatch stopwatch2 = Stopwatch.StartNew();
            int lastOperationTookTime = 0;

            Log(CC.Red, $"SC+ Loading started");

            foreach (GameObject rootGo in GetRootParents())
            {
                HashSet<GameObject> result = new();

                foreach (var entry in CarryableData.carryablePrefabDefinition)
                {
                    MelonCoroutines.Start(GetChildrenWithNameEnum(rootGo, entry.Key, result));
                    /*
                    if (carryableCoroutineCounter > cutoff)
                    {
                        carryableCoroutineCounter = 0;
                        //Log("Frame skip");
                        yield return new WaitForEndOfFrame();
                    }
                    */
                }

                while (childrenLookupCoroutineRunning > 0)
                {
                    yield return null;
                }
                Log(CC.Yellow, $"SC+ Children of {rootGo.name} lookup time: {stopwatch.ElapsedMilliseconds - lastOperationTookTime} ms");
                lastOperationTookTime = (int)stopwatch.ElapsedMilliseconds;

                foreach (GameObject child in result)
                {


                    if (child.IsNullOrDestroyed() || !child.active) continue;

                    DecorationItem di = SCPMain.MakeIntoDecoration(child);

                    if (dataList.Count == 0) continue;

                    for (int i = dataList.Count - 1; i >= 0; i--)
                    {
                        var data = dataList[i];

                        CarryableData.carryablePrefabDefinition.TryGetValue(data.name, out ObjectToModify? otm);

                        if (child.name.Contains(data.name))
                        {
                            // object still in native scene
                            if (data.IsInNativeScene() && ((data.state & CS.Removed) == 0)) //  && !data.TryGetContaier() 
                            {
                                if (WithinDistance(data.originalPos, child.transform.position))
                                {
                                    if (otm != null && otm.alwaysReplaceAfterFirstInteraction)
                                    {
                                        Log(CC.DarkCyan, $"In native scene, force removed {data.name} native: {data.nativeScene} current: {data.currentScene}");
                                        child.active = false;
                                        continue;
                                    }
                                    // in native scene and in container
                                    if (di && ((data.state & CS.InContainer) == CS.InContainer))
                                    {
                                        Log(CC.DarkCyan, $"In native scene, in container {data.name} native: {data.nativeScene} current: {data.currentScene}");
                                        data.TryGetContainer().AddDecorationItem(di);
                                    }
                                    else
                                    {
                                        Log(CC.DarkCyan, $"In native scene, moved {data.name} native: {data.nativeScene} current: {data.currentScene}");
                                    }

                                    SCPlusCarryable c = child.AddComponent<SCPlusCarryable>();
                                    CarryableManager.Add(c);
                                    c.FromProxy(data, true, true);
                                    dataList.RemoveAt(i);
                                }
                            }
                            // object no longer in native scene, but player is
                            else if (CurrentlyInNativeScene(data.nativeScene))
                            {
                                if (WithinDistance(data.originalPos, child.transform.position))
                                {
                                    if ((data.state & CS.OnPlayer) == 0) // native object moved out of scene
                                    {
                                        if (!child.GetComponent<SCPlusCarryable>())
                                        {
                                            SCPlusCarryable c = child.AddComponent<SCPlusCarryable>();
                                            c.FromProxy(data, true, false, true);
                                            CarryableManager.Add(c);
                                            Log(CC.DarkYellow, $"Not in native scene or disabled, removed and enlisted {data.name} native: {data.nativeScene} current: {data.currentScene}");
                                        }
                                        else
                                        {
                                            Log(CC.DarkMagenta, $"Not in native scene or disabled, removed {data.name} native: {data.nativeScene} current: {data.currentScene}");
                                        }

                                        child.active = false;
                                        dataList.RemoveAt(i);
                                    }
                                }
                            }
                        }
                    }
                }
                Log(CC.Green, $"SC+ Children of {rootGo.name} operations time: {stopwatch.ElapsedMilliseconds - lastOperationTookTime} ms");
                lastOperationTookTime = (int)stopwatch.ElapsedMilliseconds;
            }

            stopwatch.Stop();
            Log(CC.Red, $"SC+ Loading pass 1: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedTicks} ticks)");

            if (dataList.Count == 0) yield break;

            stopwatch.Restart();

            //SCPMain.instantiatingCarryables = true;
            GameObject globalParent = new GameObject("CarryableTemp");
            globalParent.SetActive(false);
            // instantiating remaining objects: in inventory, containers or different scene
            foreach (var data in dataList)
            {
                //MelonLogger.Msg(CC.Red, $"{data.name}");
                CarryableData.carryablePrefabDefinition.TryGetValue(data.name, out ObjectToModify? otm);
                if (otm == null) continue;

                GameObject instance = AssetHelper.SafeInstantiateAssetAsync(otm.existingDecoration ? data.name : otm.assetPath, globalParent.transform).WaitForCompletion();

                if (otm.needsReconstruction)
                {
                    instance = otm.reconstructAction.Invoke();
                }

                // prepare new instance
                switch (data.type)
                {
                    case CT.FlareGunCase:
                        foreach (PrefabSpawn ps in instance.GetComponentsInChildren<PrefabSpawn>())
                        {
                            ps.m_SpawnComplete = true;
                        }
                        break;
                    case CT.MillingMachine:
                        MelonCoroutines.Start(PrepareMillingMachine(instance));

                        break;
                    default:
                        break;
                }

                if (instance != null)
                {
                    instance.name = data.name;
                    DecorationItem di = SCPMain.MakeIntoDecoration(instance);
                    SCPlusCarryable carryable = instance.AddComponent<SCPlusCarryable>();
                    carryable.isInstance = true;
                    bool shouldLoadAdditionalData = false;

                    if ((data.state & CS.OnPlayer) == CS.OnPlayer) // in inventory
                    {
                        //dupes when changing scene
                        Log(CC.Gray, $"Instantiating object in inventory | {data.name} native: {data.nativeScene} current: {data.currentScene}");
                        GameManager.GetInventoryComponent().AddDecoration(di);
                        instance.SetActive(false);
                    }
                    else if (data.TryGetContainer()) // in container
                    {
                        Log(CC.Gray, $"Instantiating object in container | {data.name} native: {data.nativeScene} current: {data.currentScene}");
                        data.TryGetContainer().AddDecorationItem(di);
                        instance.SetActive(false);
                    }
                    else //if (!data.IsInNativeScene() && GameManager.CompareSceneNames(GameManager.m_ActiveScene, data.currentScene)) // in different scene, or same scene but spawned additionally with console
                    {
                        Log(CC.Gray, $"Instantiating object in world | {data.name} native: {data.nativeScene} current: {data.currentScene}");
                        GameObject root = PlaceableManager.FindOrCreateCategoryRoot();
                        instance.transform.SetParent(root.transform);

                        shouldLoadAdditionalData = true;
                    }

                    CarryableManager.Add(carryable);
                    carryable.FromProxy(data, true);
                    if (shouldLoadAdditionalData) carryable.RetrieveAdditionalData(); // data.dataToSave
                }
                else
                {
                    Log(CC.Red, $"Failed to instantiate {data.name}, check path: {otm.assetPath}");
                }
            }
            GameObject.Destroy(globalParent);


            stopwatch.Stop();
            stopwatch2.Stop();
            Log(CC.Red, $"SC+ Loading pass 2: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedTicks} ticks)");
            Log(CC.Red, $"SC+ Total loading: {stopwatch2.ElapsedMilliseconds} ms ({stopwatch2.ElapsedTicks} ticks)");

            yield break;
        }

        public static DecorationItem? MakeIntoDecoration(GameObject go)
        {
            if (go && !go.GetComponentInChildren<DecorationItem>())
            {
                string name = SanitizeObjectName(go.name);

                LocalizedString ls = TryGetLocalizedName(go);
                DecorationItem di = go.AddComponent<DecorationItem>();
                go.layer = vp_Layer.InteractiveProp;

                di.m_DecorationPrefab = new(name);
                di.GetDecorationPrefab();
                di.m_DisplayName = ls;//bd ? bd.m_LocalizedDisplayName : new LocalizedString() { m_LocalizationID = "NaN" };
                di.GetCraftingDisplayName();
                //di.m_IconReference = new("");

                RelevantSetupForDecorationItem(di);

                if (CarryableData.carryablePrefabDefinition.ContainsKey(name) && CarryableData.carryablePrefabDefinition[name].pickupable == false)
                { 
                    di.m_AllowInInventory = false;
                }
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
                DecorationItem di = go?.GetComponent<DecorationItem>();
                if (di)
                {
                    GameObject dupe = GameObject.Instantiate(go);
                    dupe.name = SanitizeObjectName(dupe.name);
                    GameManager.GetPlayerManagerComponent().StartPlaceMesh(dupe, PlaceMeshFlags.DestroyOnCancel, di.m_PlacementRules);
                    justDupedContainer = true;
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
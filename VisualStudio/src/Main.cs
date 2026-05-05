using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using Il2CppTLD.OptionalContent;

namespace SCPlus
{
    public class SCPMain : MelonMod
    {
        public static bool isLoaded = false;
        public static bool hasTFTFTF;

        public static bool DEVInspectMode;
        public static float DEVInspectModeMoveStep = 0.01f;
        public static GameObject DEVInspectTempGO;

        public static IResourceLocator catalogLocator;
        public static Dictionary<string, string> catalogParsed = new();
        public static HashSet<string> iconGuidLookupList = new();

        public static bool decorationListPopulated;

        public static Transform car;

        public override void OnInitializeMelon()
        {
            //modsPath = Path.GetFullPath(typeof(MelonMod).Assembly.Location + "/../../../Mods/");
            LocalizationManager.LoadJsonLocalization(ResourceHandler.LoadEmbeddedJSON("Localization.json"));
            BreakDownHelper.PopulateDefinitions();
            CarryableData.sneakyBundle = ResourceHandler.LoadEmbeddedAssetBundle("nothingtoseehere");

            Settings.OnLoad();

            ResourceHandler.ExtractFolderFromResources(Path.Combine(modsPath, modFolder), resourcesFolder + resourcesFolderForAssumingPlayersUnableToRead, true);

            AsyncOperationHandle<IResourceLocator> handle = null;
            try
            {
                string path = Path.Combine(modsPath, iconsFolder, iconsCatalog + ".json");
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

            LookupPotentialCarryables(sceneName);
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            DecorationPatches.injectPdids = true;
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



            float moveSpeed = 10f; 
            float turnSpeed = 5f; 

            if (GetKeyHeld(KeyCode.W) || GetKeyHeld(KeyCode.S))
            {
                if (GameManager.GetPlayerManagerComponent()?.GetControlMode() == PlayerControlMode.InVehicle)
                {
                    var player = GameManager.GetPlayerTransform();
                    if (player == null || car == null)
                        return;

                    Vector3 moveDir = (GetKeyHeld(KeyCode.W) ? player.forward : -player.forward);
                    Vector3 delta = moveDir * moveSpeed * Time.deltaTime;

                    // move both
                    car.position += delta;
                    player.position += delta;

                    // rotate car gradually toward movement direction
                    if (moveDir.sqrMagnitude > 0.0001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                        car.rotation = Quaternion.Slerp(car.rotation, targetRot, turnSpeed * Time.deltaTime);
                    }
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
                    GreenScreen.TakeScreenshot();
                }
            }

        }
    }
}
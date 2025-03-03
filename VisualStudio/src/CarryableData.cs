using Unity.VisualScripting;

namespace SCPlus
{
    internal class CarryableSaveDataProxy
    {
        public CS state = 0;
        //public bool disabled = false;
        public string name = "";
        public CT type = 0;
        public string nativeScene = "";
        public string currentScene = "";
        public Vector3 originalPos;
        public Vector3 currentPos;
        public Quaternion currentRot;
        public string dataToSave = "";
        public string guid = "";
        public string containerGuid = "";
        //public bool onPlayer = false;

        public bool IsInNativeScene()
        {
            if (string.IsNullOrEmpty(currentScene)) return false;
            return nativeScene.Contains(currentScene);
        }

        public Container? TryGetContainer()
        {
            if (string.IsNullOrEmpty(containerGuid) || containerGuid == missingGuid) return null;

            return PdidTable.GetGameObject(containerGuid)?.GetComponent<Container>();
        }
    }


    internal class ObjectToModify
    {
        public CT type = CT.Basic;
        public string assetPath = "";
        public bool needsReconstruction = false;
        public Func<GameObject> reconstructAction;
        public bool existingDecoration = false;
        public bool alwaysReplaceAfterFirstInteraction = false;
        public bool pickupable = true;
    }

    internal class OverrideData
    {
        public string nameLocID = "";
        public float weight = 0f;
        public Vector3 placementOffset = Vector3.zero;
    }

    internal class CarryableData
    {
        public static float carriedObjectWeight = 0f;
        public enum CarryableType
        {
            Basic,
            Stove,
            WaterSource,
            TraderRadio,
            TreeLimb,
            Forge,
            AmmoWorkbench,
            MillingMachine,
            FlareGunCase,
            Container
        }

        [Flags]
        public enum CarryableState
        {
            None = 0,
            Removed = 1,
            OnPlayer = 2,
            Dismantled = 4,
            InContainer = 8,
            ExistingDecoration = 16
        }

        public static SCPlusCarryable? SetupCarryable(DecorationItem di, bool enlist)
        {

            foreach (var entry in CarryableData.carryablePrefabDefinition)
            {
                if (di.name.ToLower().Contains(entry.Key.ToLower()))
                {
                    SCPlusCarryable carryable = di.GetOrAddComponent<SCPlusCarryable>();
                    if (string.IsNullOrEmpty(carryable.objectName)) carryable.objectName = SanitizeObjectName(di.name);
                    if (carryable.originalPos == Vector3.zero) carryable.originalPos = di.transform.position;
                    if (string.IsNullOrEmpty(carryable.nativeScene)) carryable.nativeScene = di.gameObject.scene.name;
                    carryable.type = entry.Value.type;

                    if (enlist)
                    {
                        CarryableManager.Add(carryable);
                    }

                    //Il2Cpp.GameManager.GetPlayerManagerComponent().StartPlaceMesh(UnityEngine.AddressableAssets.Addressables.InstantiateAsync("").WaitForCompletion(), 0);
                    //UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Texture2D>("Assets/ArtAssets/Textures/Unique/OBJ_HouseInteriorLights_A02.tga").WaitForCompletion();

                    return carryable;
                }
            }
            return null;
        }

        public static string[] blacklist = new string[]
        {
            "CONTAINER_CacheStoreCommon",
        };

        public static Dictionary<string, ObjectToModify> carryablePrefabDefinition = new(StringComparer.OrdinalIgnoreCase)
        {
            // radio
            {"INTERACTIVE_SURVIVAL_TraderRadio", new() {assetPath = "Assets/ArtAssets/Characters/NPC/Props/Survival_TraderRadio.prefab", type = CT.TraderRadio, pickupable = false} },

            // stoves
            {"INTERACTIVE_StoveMetalA", new() {assetPath = "Assets/Prefabs/Interactive/INTERACTIVE_StoveMetalA.prefab", type = CT.Stove} },
            {"INTERACTIVE_PotBellyStove", new() {assetPath = "Assets/Prefabs/Interactive/INTERACTIVE_PotBellyStove.prefab", type = CT.Stove} },
            {"INTERACTIVE_StoveWoodC", new() {assetPath = "", type = CT.Stove, needsReconstruction = true, reconstructAction = () => ReconstructStoveWood()} },
            {"INTERACTIVE_FireBarrel", new() {type = CT.Stove, existingDecoration = true, alwaysReplaceAfterFirstInteraction = true } },
            {"INTERACTIVE_RimGrill", new() {type = CT.Stove, existingDecoration = true} },

            // containers
            {"CONTAINER_FlareGun", new() {assetPath = "Assets/Prefabs/Containers/CONTAINER_FlareGun.prefab", type = CT.FlareGunCase, alwaysReplaceAfterFirstInteraction = true} },
            {"CONTAINER_ForestryCrate", new() {assetPath = "CONTAINER_ForestryCrate", type = CT.Container} }, // Assets/Prefabs/Containers/CONTAINER_ForestryCrate.prefab why the fuck?
            {"CONTAINER_ForestryCrateB", new() {assetPath = "Assets/Prefabs/Containers/CONTAINER_ForestryCrateB.prefab", type = CT.Container} },
            {"CONTAINER_FirewoodBin", new() {assetPath = "Assets/Prefabs/Containers/CONTAINER_FirewoodBin.prefab", type = CT.Container} },

            // workbenches
            {"INTERACTIVE_Forge", new() {assetPath = "Assets/Prefabs/Interactive/INTERACTIVE_Forge.prefab", type = CT.Stove} },
            {"INTERACTIVE_AmmoWorkBench", new() {assetPath = "Assets/Prefabs/Interactive/INTERACTIVE_AmmoWorkBench.prefab", type = CT.AmmoWorkbench} },
            {"INTERACTIVE_IndustrialMillingMachine", new() {assetPath = "Assets/ArtAssets/Env/Objects/OBJ_IndustrialMillingMachine/OBJ_IndustrialMillingMachine_Prefab.prefab", type = CT.MillingMachine} },

            // misc
            {"OBJ_GravityToilet", new() {assetPath = "", type = CT.WaterSource, needsReconstruction = true} },
            //OBJ_ChandelierA_Prefab (fbx only and sway animation, ref in campoffice)
            // sink
            // OBJ_ElectricGenerator_A_Prefab


        };

        public static Dictionary<string, OverrideData> decorationOverrideData = new()
        {
            {"INTERACTIVE_SURVIVAL_TraderRadio", new() {nameLocID = "SCP_Deco_TraderRadio", weight = 4f} },
            {"CONTAINER_FlareGun", new() {nameLocID = "SCP_Deco_FlareGunCase", weight = 4f} },
            {"INTERACTIVE_AmmoWorkBench", new() {nameLocID = "SCP_Deco_AmmoWorkbench", weight = 40f, placementOffset = Vector3.up * 0.35f} },
            {"INTERACTIVE_FireBarrel", new() {weight = 10f} },
            {"INTERACTIVE_PotBellyStove", new() {weight = 30f} },
            {"INTERACTIVE_StoveWoodC", new() {weight = 30f} },
            {"INTERACTIVE_StoveMetalA", new() {weight = 50f} },
            {"INTERACTIVE_Forge", new() {weight = 50f} },
            {"INTERACTIVE_IndustrialMillingMachine", new() {weight = 50f, placementOffset = Vector3.up * 0.75f} },
            {"CONTAINER_ForestryCrate", new() {weight = 10f} },
            {"CONTAINER_ForestryCrateB", new() {weight = 10f} },
            {"CONTAINER_FirewoodBin", new() {weight = 10f} },
            {"CONTAINER_CacheStoreCommon", new() {weight = 0.5f} },
            {"OBJ_Piano_Prefab", new() { weight = 40f } },
            {"OBJ_CurtainStage_Prefab", new() { weight = 10f } },
            {"OBJ_ClothesHanger_Prefab", new() { weight = 0.1f }},
        };

        public static GameObject ReconstructStoveWood()
        {
            GameObject go = new GameObject("INTERACTIVE_StoveWoodC");
            GameObject main = AssetHelper.SafeInstantiateAssetAsync("Assets/ArtAssets/Env/Objects/OBJ_StoveWoodC/OBJ_StoveWoodCMain.FBX", go.transform).WaitForCompletion();
            GameObject stove = new GameObject("Stove");
            stove.transform.parent = go.transform;
            GameObject hinge = new GameObject("Hinge");
            hinge.transform.parent = stove.transform;
            hinge.transform.localPosition = new Vector3(0.3374f, 0.4804f, -0.3461f);
            GameObject door = AssetHelper.SafeInstantiateAssetAsync("Assets/ArtAssets/Env/Objects/OBJ_StoveWoodC/OBJ_StoveWoodCDoor.FBX", hinge.transform).WaitForCompletion();

            MeshCollider collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = AssetHelper.SafeLoadAssetAsync<Mesh>("Assets/ArtAssets/Env/Objects/OBJ_StoveWoodC/OBJ_StoveWoodCMain_Col.FBX").WaitForCompletion();

            // materials

            Material material = AssetHelper.SafeLoadAssetAsync<Material>("Assets/ArtAssets/Env/Objects/Materials/OBJ_StoveWoodB_Dif.mat").WaitForCompletion();

            Renderer[] renderers = [main.GetComponent<MeshRenderer>(), door.GetComponent<MeshRenderer>()];
            foreach (Renderer r in renderers)
            {
                r.sharedMaterial = material;
            }

            // animation

            iTweenEvent iteOpen = hinge.AddComponent<iTweenEvent>();
            iteOpen.Initialize("open", -0.083f); 
            iTweenEvent iteClose = hinge.AddComponent<iTweenEvent>();
            iteClose.Initialize("close", 0.083f);

            ObjectAnim oa = stove.AddComponent<ObjectAnim>();
            oa.Initialize(hinge);

            // fire

            GameObject fireDonor = AssetHelper.SafeInstantiateAssetAsync("Assets/Prefabs/Interactive/INTERACTIVE_PotBellyStove.prefab").WaitForCompletion();
            Transform fireRoot = GameObject.Instantiate(fireDonor.transform.Find("Fire"), stove.transform);
            fireRoot.name = "Fire";
            //fireRoot.parent = stove.transform;
            GameObject cookingPointIndicator = fireDonor.transform.Find("PlacePoints/Cylinder").gameObject;
            cookingPointIndicator.transform.parent = go.transform;
            cookingPointIndicator.transform.localPosition = Vector3.zero;

            GameObject.Destroy(fireDonor);

            fireRoot.localPosition = new Vector3(0.06f, 0f, 0.001f);
            Vector3 firePosition = new Vector3(0, 0.2f, -0.2f);
            foreach (Transform t in fireRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLower().StartsWith("fx_stage"))
                {
                    t.localPosition = firePosition;
                    t.localScale = new Vector3(2f, 0.4f, 1f);
                    t.Find("Flame core 2")?.gameObject.SetActive(false);
                }
            }

            Fire fire = fireRoot.GetComponent<Fire>();

            // light
            
            Transform fxLighting = fireRoot.transform.Find("FX_Lighting");
            MelonCoroutines.Start(SetupStoveLight(fxLighting));

            // components

            WoodStove ws = stove.AddComponent<WoodStove>();
            ws.m_StoveCloseSound = "Play_SndMechWoodstove02Close";
            ws.m_StoveOpenSound = "Play_SndMechWoodstove02Open";
            ws.Fire = fire;
            ws.m_ObjectAnim = oa;
            ws.m_DefaultHoverText = new LocalizedString() { m_LocalizationID = "GAMEPLAY_WoodStove" };
            ws.enabled = true;

            ActiveBurner ab = stove.AddComponent<ActiveBurner>();
            ab.m_Fire = fire;
            ab.m_Rend = main.GetComponent<Renderer>();
            ab.m_Material = ab.m_Rend.sharedMaterial;
            ab.m_OpacityRange = new MinMax(0f, 1f);

            // cooking slots

            Transform placePointRoot = new GameObject("PlacePoints").transform;
            placePointRoot.parent = go.transform;

            GameObject pointLeft = new GameObject("Left");
            pointLeft.transform.parent = placePointRoot;
            pointLeft.transform.localPosition = new Vector3(-0.217f, 0.8275f, -0.09f);
            GameObject pointRight = new GameObject("Right");
            pointRight.transform.parent = placePointRoot;
            pointRight.transform.localPosition = new Vector3(0.205f, 0.8275f, -0.085f);

            GameObject[] points = [pointLeft, pointRight];
            List<CookingSlot> css = new();
            List<Renderer> indicators = new();

            foreach (GameObject p in points)
            {
                GearPlacePoint gpp = p.AddComponent<GearPlacePoint>();
                gpp.m_AuthorizedGearPrefabs = new();
                gpp.m_AllowAllCookingItems = true;
                gpp.m_FireToAttach = fire;

                GameObject indicator = GameObject.Instantiate(cookingPointIndicator, p.transform);
                indicator.name = "Indicator";
                indicator.transform.localScale = new Vector3(0.2451f, 0.0026f, 0.2451f);

                CookingSlot slot = indicator.GetComponent<CookingSlot>();
                slot.m_GearPlacePoint = gpp;
                slot.SetFireplaceHost(ws);

                css.Add(slot);
                indicators.Add(indicator.GetComponent<Renderer>());
            }

            GameObject.Destroy(cookingPointIndicator);

            PlacePoints pp = placePointRoot.gameObject.AddComponent<PlacePoints>();
            pp.m_PlacePoints = indicators.ToArray();

            ws.m_CookingSlots = css.ToArray();

            GameObject goa = Resources.Load<GameObject>("Assets/PrefabInstance/INTERACTIVE_StoveWoodC_LOD0");
            
            return go;
        }

        public static IEnumerator SetupStoveLight(Transform lightRoot)
        {
            yield return new WaitForEndOfFrame();

            lightRoot.localPosition = Vector3.zero;
            Light cookieLight = lightRoot.GetChild(0).GetComponent<Light>();
            Light staticLight = lightRoot.GetChild(1).GetComponent<Light>();
            LightDancing animatedLight1 = lightRoot.GetChild(2).GetComponent<LightDancing>();
            LightDancing animatedLight2 = lightRoot.GetChild(3).GetComponent<LightDancing>();
            staticLight.range = 0.5f;
            staticLight.transform.localPosition = Vector3.up * 0.45f;
            //animatedLight1.Update();
            animatedLight1.originalPosition = new Vector3(-0.05f, 0.4725f, -0.3f);
            animatedLight1.shiftRate = 4f;
            animatedLight1.GetComponent<LightFadeFire>().originalBrightness = 2.5f;
            //animatedLight2.Update();
            animatedLight2.originalPosition = new Vector3(0.05f, 0.4725f, -0.3f);
            animatedLight2.shiftRate = 3f;
            animatedLight2.GetComponent<LightFadeFire>().originalBrightness = 1f;
            animatedLight2.GetComponent<Light>().range = 5f;

            cookieLight.transform.localPosition = new Vector3(-0.0029f, 0.4737f, -0.338f);
            cookieLight.cookie = AssetHelper.SafeLoadAssetAsync<Texture2D>("Assets/ArtAssets/Textures/FX/FX_LightCookieA_Dif.tga").WaitForCompletion();
            cookieLight.spotAngle = 100f;
            cookieLight.innerSpotAngle = 80f;

            yield break;
        }
    }
}

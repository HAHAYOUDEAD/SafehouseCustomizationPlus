using Il2CppTLD.ModularElectrolizer;
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
        //public bool alwaysReplaceAfterFirstInteraction = false;
        public bool pickupable = true; // can't dupe/spawn
    }

    internal class OverrideData
    {
        public string nameLocID = "";
        public float weight = 0f;
        public Vector3 placementOffset = Vector3.zero;
    }

    internal class BlacklistObject
    {
        public string name;
        public Vector3 pos;
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
            {"INTERACTIVE_SURVIVAL_TraderRadio", new() {assetPath = "", type = CT.TraderRadio, pickupable = false } },//needsReconstruction = true, reconstructAction = () => ReconstructTraderRadio()} },

            // stoves
            {"INTERACTIVE_StoveMetalA", new() {assetPath = "Assets/Prefabs/Interactive/INTERACTIVE_StoveMetalA.prefab", type = CT.Stove} },
            {"INTERACTIVE_PotBellyStove", new() {assetPath = "Assets/Prefabs/Interactive/INTERACTIVE_PotBellyStove.prefab", type = CT.Stove} },
            {"INTERACTIVE_StoveWoodC", new() {assetPath = "", type = CT.Stove, needsReconstruction = true, reconstructAction = () => ReconstructStoveWood()} },
            {"INTERACTIVE_FireBarrel", new() {type = CT.Stove, existingDecoration = true } },
            {"INTERACTIVE_RimGrill", new() {type = CT.Stove, existingDecoration = true} },

            // containers
            {"CONTAINER_FlareGun", new() {assetPath = "Assets/Prefabs/Containers/CONTAINER_FlareGun.prefab", type = CT.FlareGunCase } },
            {"CONTAINER_ForestryCrate", new() {assetPath = "CONTAINER_ForestryCrate", type = CT.Container} }, // Assets/Prefabs/Containers/CONTAINER_ForestryCrate.prefab why the fuck?
            {"CONTAINER_ForestryCrateB", new() {assetPath = "Assets/Prefabs/Containers/CONTAINER_ForestryCrateB.prefab", type = CT.Container} },
            {"CONTAINER_FirewoodBin", new() {assetPath = "Assets/Prefabs/Containers/CONTAINER_FirewoodBin.prefab", type = CT.Container} },

            // workbenches
            {"INTERACTIVE_Forge", new() {assetPath = "Assets/Prefabs/Interactive/INTERACTIVE_Forge.prefab", type = CT.Stove} },
            {"INTERACTIVE_AmmoWorkBench", new() {assetPath = "Assets/Prefabs/Interactive/INTERACTIVE_AmmoWorkBench.prefab", type = CT.AmmoWorkbench} },
            {"INTERACTIVE_IndustrialMillingMachine", new() {assetPath = "Assets/ArtAssets/Env/Objects/OBJ_IndustrialMillingMachine/OBJ_IndustrialMillingMachine_Prefab.prefab", type = CT.MillingMachine} },

            // misc
            //{"OBJ_GravityToilet", new() {assetPath = "", type = CT.WaterSource, needsReconstruction = true} },
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
            {"INTERACTIVE_Forge", new() {nameLocID = "SCP_Deco_Forge", weight = 50f} },
            {"INTERACTIVE_IndustrialMillingMachine", new() {weight = 50f, placementOffset = Vector3.up * 0.75f} },
            {"CONTAINER_ForestryCrate", new() {weight = 10f} },
            {"CONTAINER_ForestryCrateB", new() {weight = 10f} },
            {"CONTAINER_FirewoodBin", new() {weight = 10f} },
            {"CONTAINER_CacheStoreCommon", new() {weight = 0.5f} },
            {"OBJ_Piano_Prefab", new() { weight = 40f } },
            {"OBJ_CurtainStage_Prefab", new() { weight = 10f } },
            {"OBJ_ClothesHanger_Prefab", new() { weight = 0.1f }},
        };

        public static Dictionary<string, HashSet<BlacklistObject>> blacklistSpecific = new(StringComparer.OrdinalIgnoreCase)
        {
            /*
            { "CanyonRoadTransitionZone", 
                [
                 new() { name = "OBJ_FishingCabinDresser", pos = new Vector3(233.2839f, 40.3067f, 421.9208f) }, 
                 new() { name = "OBJ_FishingCabinCupboard", pos = new Vector3(232.6224f, 40.2873f, 423.2194f) }
                ] 
            }
            */
        };
    }
}

global using HarmonyLib;
global using Il2Cpp;
global using Il2CppRewired.Utils;
global using Il2CppSystem.Linq;
global using Il2CppTLD.IntBackedUnit;
global using Il2CppTLD.Interactions;
global using Il2CppTLD.PDID;
global using Il2CppTLD.Placement;
global using Il2CppTLD.Trader;
global using Il2CppTLD.Utility;
global using Il2CppVLB;
global using LocalizationUtilities;
global using MelonLoader;
global using MelonLoader.TinyJSON;
global using ModData;
global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Reflection;
global using System.Text;
global using System.Text.RegularExpressions;
global using UnityEngine;
global using UnityEngine.AddressableAssets;
global using UnityEngine.Events;
global using UnityEngine.SceneManagement;
global using static Il2Cpp.Utils;
global using static SCPlus.Utility;
global using AssetHelper = Il2CppTLD.AddressableAssets.AssetHelper;
global using CC = System.ConsoleColor;
global using CS = SCPlus.CarryableData.CarryableState;
global using CT = SCPlus.CarryableData.CarryableType;
global using SceneManager = UnityEngine.SceneManagement.SceneManager;
using Il2CppTLD.ModularElectrolizer;
using System.IO.Compression;

namespace SCPlus
{
    internal static class Utility
    {
        public const string modVersion = "1.8.1";
        public const string modName = "SafehouseCustomizationPlus";
        public const string modAuthor = "Waltz";

        public const string resourcesFolder = "SafehouseCustomizationPlus.Resources.";
        public const string iconsFolder = "SCPlus/Icons/";
        public const string iconsCatalog = "catalog_SCPlusIcons";
        public const string placeholderIconName = "placeholder";


        public const string dataSeparator = "_|_";

        public static ModDataManager dataManager = new ModDataManager(modName);
        public static readonly string fireSaveDataTag = "fireBarrels";
        public static readonly string movablesSaveDataTag = "carryables";


        public static readonly string missingGuid = "MISSING GUID";



        public static bool IsScenePlayable()
        {
            return !(string.IsNullOrEmpty(GameManager.m_ActiveScene) || GameManager.m_ActiveScene.Contains("MainMenu") || GameManager.m_ActiveScene == "Boot" || GameManager.m_ActiveScene == "Empty");
        }

        public static bool IsScenePlayable(string scene)
        {
            return !(string.IsNullOrEmpty(scene) || scene.Contains("MainMenu") || scene == "Boot" || scene == "Empty");
        }

        public static bool IsMainMenu(string scene)
        {
            return !string.IsNullOrEmpty(scene) && scene.Contains("MainMenu");
        }

        public static GameObject GetInteractiveGameObjectUnderCrosshair()
        {
            GameObject go = null;
            PlayerManager pm = GameManager.GetPlayerManagerComponent();

            float maxPickupRange = GameManager.GetGlobalParameters().m_MaxPickupRange;
            float maxRange = pm.ComputeModifiedPickupRange(maxPickupRange);
            if (pm.GetControlMode() == PlayerControlMode.InFPCinematic)
            {
                maxRange = 50f;
            }

            go = GameManager.GetPlayerManagerComponent().GetInteractiveObjectUnderCrosshairs(maxRange);

            return go;
        }

        public static GameObject? GetRealGameObjectUnderCrosshair()
        {
            PlayerManager pm = GameManager.GetPlayerManagerComponent();

            float maxPickupRange = GameManager.GetGlobalParameters().m_MaxPickupRange;
            float maxRange = pm.ComputeModifiedPickupRange(maxPickupRange);
            if (pm.GetControlMode() == PlayerControlMode.InFPCinematic)
            {
                maxRange = 50f;
            }

            Ray ray = GameManager.GetMainCamera().ScreenPointToRay(Input.mousePosition);
            int layerMask = 0;// Physics.AllLayers;

            //layerMask ^= (1 << layerToExclude);
            //layerMask |= (1 << layerToInclude);

            layerMask |= (1 << vp_Layer.Default);
            layerMask |= (1 << vp_Layer.InteractiveProp);
            layerMask |= (1 << vp_Layer.Container);
            layerMask |= (1 << vp_Layer.TerrainObject);

            if (Physics.Raycast(ray, out RaycastHit hit, maxRange, layerMask))
            {
                GameObject hitGo = GetRealParent(hit.transform);
                if (hitGo.name.StartsWith("ARC_"))
                {
                    HUDMessage.AddMessage(Localization.Get("SCP_Action_NoValidObject"));
                    return null;
                }
                foreach (Renderer r in hitGo.transform.GetComponentsInChildren<Renderer>())
                {
                    if (r && r.isPartOfStaticBatch)
                    {
                        HUDMessage.AddMessage(Localization.Get("SCP_Action_IsPartOfStaticBatch"));
                        return null;
                    }
                }
                //HUDMessage.AddMessage("Hit: " + GetRealParent(hit.transform)?.name);
                return hitGo;
            }
            else
            {
                HUDMessage.AddMessage(Localization.Get("SCP_Action_NoValidObject"));
                return null;
            }
        }
        /*
        public static string testtest(Vector3 v)
        { 
            return v.ToJsonSkipDefaults();
        }

        public static string testtsdvest(CarryableSaveDataProxy v)
        {
            return v.ToJsonSkipDefaults();
        }
        */
        public static string JsonDumpSkipDefaults<T>(this T obj) // thanks GPT
        {
            if (obj == null) return "null";

            var type = typeof(T);
            var properties = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            StringBuilder jsonBuilder = new StringBuilder();
            jsonBuilder.Append('{');

            bool firstProperty = true;

            jsonBuilder.Append($"\"@type\":{JSON.Dump(type.FullName)},");

            foreach (var prop in properties)
            {
                object value = prop.GetValue(obj);
                object defaultValue = GetDefaultValue(prop.FieldType);

                // Skip default values and empyt strings
                if (value == null || value.Equals(defaultValue)) continue;
                if (prop.FieldType == typeof(string) && value.Equals(string.Empty)) continue;

                if (!firstProperty) jsonBuilder.Append(',');
                firstProperty = false;

                jsonBuilder.Append($"\"{prop.Name}\":{JSON.Dump(value)}");
            }

            jsonBuilder.Append('}');
            return jsonBuilder.ToString();
        }

        private static object GetDefaultValue(Type t)
        {
            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }

        public static Guid GenerateSeededGuid(int seed)
        {
            var r = new System.Random(seed);
            var guid = new byte[16];
            r.NextBytes(guid);


            return new Guid(guid);
        }

        public static int SeedFromCoords(Vector3 v)
        {
            int result = Mathf.CeilToInt(v.x * v.z + v.y * 10000f);

            return result;
        }

        public static string? LoadEmbeddedJSON(string name)
        {
            name = resourcesFolder + name;

            string? result = null;

            //MemoryStream memoryStream;
            Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (stream != null)
            {
                StreamReader reader = new StreamReader(stream);
                result = reader.ReadToEnd();
            }

            return result;
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

        public static bool WithinDistance(Vector3 A, Vector3 B, float distance = 0.01f)
        {
            float dx = A.x - B.x;
            if (Mathf.Abs(dx) > distance) return false;

            float dy = A.y - B.y;
            if (Mathf.Abs(dy) > distance) return false;

            float dz = A.z - B.z;
            if (Mathf.Abs(dz) > distance) return false;

            return true;
        }

        // stolen from Remove Clutter
        internal static List<GameObject> GetRootParents()
        {
            List<GameObject> rootObj = new List<GameObject>();

            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);

                GameObject[] sceneObj = scene.GetRootGameObjects();

                foreach (GameObject obj in sceneObj)
                {
                    bool flag = obj.IsNullOrDestroyed() ||
                        obj.transform.childCount == 0 ||
                        obj.active == false ||
                        obj.name.StartsWith("SCRIPT_") ||
                        obj.name.StartsWith("CORPSE_") ||
                        obj.name.StartsWith("GEAR_") ||
                        obj.name.StartsWith("INTERACTIVE_");
                    if (flag) continue;
                    rootObj.Add(obj);
                }
            }

            return rootObj;
        }

        // stolen from Remove Clutter
        internal static void GetChildrenWithName(GameObject obj, string name, List<GameObject> result)
        {
            if (obj.transform.childCount > 0)
            {
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    GameObject child = obj.transform.GetChild(i).gameObject;

                    if (child.name.ToLower().Contains(name.ToLower()))
                    {
                        result.Add(child);

                        continue;
                    }
                    GetChildrenWithName(child, name, result);
                }
            }
        }


        public static GameObject GetRealParent(Transform t)
        {
            while (t.parent
                && !WithinDistance(t.parent.position, Vector3.zero)
                && t.parent.name.Contains("_")
                && !t.parent.GetComponent<RadialObjectSpawner>())
            {
                //MelonLogger.Msg($"< {t.name}");
                t = t.parent;
            }
            /*
            while (t.parent && t.parent.name.Contains("_"))
            {
                t = t.parent;
            }
            */

            return t.gameObject;
        }

        public static LocalizedString TryGetLocalizedName(GameObject go)
        {
            //LocalizedString ls;




            // get defaulthovertext from baseinteraction
            //Il2CppSystem.Ty



            string name = SanitizeObjectName(go.name);
            if (CarryableData.decorationOverrideData.ContainsKey(name))
            {
                string locID = CarryableData.decorationOverrideData[name].nameLocID;
                if (!string.IsNullOrEmpty(locID))
                {
                    return new LocalizedString() { m_LocalizationID = locID };
                }

            }

            BaseInteraction bi = go.GetComponentInChildren<BaseInteraction>();
            if (bi && !string.IsNullOrEmpty(bi.m_DefaultHoverText.m_LocalizationID))
            {
                return bi.m_DefaultHoverText;
            }
            BreakDown bd = go.GetComponentInChildren<BreakDown>();
            if (bd && !string.IsNullOrEmpty(bd.m_LocalizedDisplayName.m_LocalizationID))
            {
                return bd.m_LocalizedDisplayName;
            }

            if (Localization.Get(name) == name) // loc doesn't exist
            {
                //name = SanitizeObjectName(go.name);
                name = name.Split('_')[1];
                name = Regex.Replace(name, "(?<!^)([A-Z])", " $1"); // insert spaces before capital letters
                string[] s = name.Split(" ");
                if (s[s.Length - 1].Length < 2) name = name[..^2];
            }
            return new LocalizedString() { m_LocalizationID = name };
            //    if (go.GetComponentInChildren<WoodStove>()) ;
            //if (go.GetComponentInChildren<BaseInteraction>())


        }

        public static string SanitizeObjectName(string s)
        {
            s = Regex.Replace(s, @"\s|\(.*?\)", ""); // remove spaces, digits and anything within ()
            s = s.Trim(); // remove leading and trailing spaces
            s = Regex.Replace(s, @"\d+$", ""); // remove trailing digits
            return s;
        }

        public static void Log(ConsoleColor color, string message)
        {
            if (Settings.options.debugLog)
            {
                MelonLogger.Msg(color, message);
            }
        }

        public static string TryGetGuid(GameObject go)
        {
            ObjectGuid og = GetRealParent(go.transform).GetComponentInChildren<ObjectGuid>();
            return og ? og.GetPDID() : "";
        }


        public static string GetGuid(this Container c)
        {
            if (c.GetComponent<ObjectGuid>()) return c.GetComponent<ObjectGuid>().PDID;
            //if (c.GetComponentInParent<ObjectGuid>()) return c.GetComponentInParent<ObjectGuid>().PDID;
            return missingGuid;
        }


        public static Scene[] LoadedScenes()
        {
            int countLoaded = UnityEngine.SceneManagement.SceneManager.sceneCount;
            Scene[] loadedScenes = new Scene[countLoaded];

            for (int i = 0; i < countLoaded; i++)
            {
                loadedScenes[i] = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            }

            return loadedScenes;
        }

        public static string CompressDeflate(string text) // thanks Copilot
        {
            byte[] rawBytes = Encoding.UTF8.GetBytes(text);

            using (MemoryStream output = new MemoryStream())
            {
                using (DeflateStream deflate = new DeflateStream(output, CompressionMode.Compress))
                {
                    deflate.Write(rawBytes, 0, rawBytes.Length);
                }
                return Convert.ToBase64String(output.ToArray());
            }
        }

        public static string DecompressDeflate(string compressedText) // thanks Copilot
        {
            byte[] compressedBytes = Convert.FromBase64String(compressedText);

            using (MemoryStream input = new MemoryStream(compressedBytes))
            using (DeflateStream deflate = new DeflateStream(input, CompressionMode.Decompress))
            using (StreamReader reader = new StreamReader(deflate, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        public static IEnumerator PrepareMillingMachine(GameObject go)
        {
            go.name = "INTERACTIVE_IndustrialMillingMachine";
            SimpleInteraction si = go.AddComponent<SimpleInteraction>();
            si.m_DefaultHoverText = new LocalizedString() { m_LocalizationID = "GAMEPLAY_MillingMachine" };

            MillingMachine mm = go.AddComponent<MillingMachine>();
            mm.m_IdleAudioPlay = "Play_MillingMachine";
            mm.m_IdleAudioStop = "Stop_MillingMachine";
            mm.m_RepairAudioLoop = "Play_MillingMachineRepair";

            Action action = () => mm.InitializeInteraction(si);
            InvokableCall invokable = new InvokableCall(action);
            si.FindOrAddEventForEventType(InteractionEventType.InitializeInteraction).AddCall(invokable);
            si.FindOrAddEventForEventType(InteractionEventType.InitializeInteraction).m_Calls.AddListener(invokable);

            action = () => mm.PerformInteraction();
            invokable = new InvokableCall(action);
            si.FindOrAddEventForEventType(InteractionEventType.PerformInteraction).AddCall(invokable);
            si.FindOrAddEventForEventType(InteractionEventType.PerformInteraction).m_Calls.AddListener(invokable);

            yield return new WaitForEndOfFrame();

            mm.InitializeInteraction(si);

            AuroraModularElectrolizer ame = go.GetComponent<AuroraModularElectrolizer>();
            if (ame)
            {
                ame.Initialize();
                ame.InitializeFlickerSet();
                ame.Awake();
            }
            yield break;
        }

        public static bool CurrentlyInNativeScene(SCPlusCarryable sc) => SceneManager.GetAllScenes().Contains(SceneManager.GetSceneByName(sc.nativeScene));
        public static bool CurrentlyInNativeScene(string native) => SceneManager.GetAllScenes().Contains(SceneManager.GetSceneByName(native));

        public static iTweenEvent Initialize(this iTweenEvent ite, string name, float rotation, float speed = 1f)
        {
            ite.animating = false;
            ite.playAutomatically = false;
            ite.tweenName = name;
            ite.type = iTweenEvent.TweenType.RotateBy;
            ite.vector3s = new Vector3[] { new Vector3(0, rotation, 0) };
            ite.floats = new float[] { speed };
            ite.keys = new string[] { "time", "amount" };
            ite.indexes = new int[] { 0, 0 };

            return ite;
        }

        public static ObjectAnim Initialize(this ObjectAnim oa, GameObject target)
        {
            oa.m_Target = target;
            oa.Start();

            return oa;
        }
    }
}

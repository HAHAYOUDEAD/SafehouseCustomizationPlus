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
//global using MelonLoader.TinyJSON;
global using TLD.TinyJSON;
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
using System.Diagnostics;
using System.IO.Compression;
using UnityEngine.Rendering;

namespace SCPlus
{
    public static class Extensions
    {
        public static bool TryGetGuid(this Container c, out string guid)
        {
            if (c.GetComponent<ObjectGuid>())
            {
                guid = c.GetComponent<ObjectGuid>().PDID;
                return true;
            }
            
            guid = missingGuid;
            return false;
        }

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
                object defaultValue = prop.FieldType.GetDefaultValue();

                // Skip default values and empyt strings
                if (value == null || value.Equals(defaultValue)) continue;
                if (prop.FieldType == typeof(string) && value?.Equals(string.Empty) == true) continue;
                //if (prop.FieldType == typeof(int) && value?.Equals(-1) == true) continue;

                if (!firstProperty) jsonBuilder.Append(',');
                firstProperty = false;

                jsonBuilder.Append($"\"{prop.Name}\":{JSON.Dump(value)}");
            }

            jsonBuilder.Append('}');
            return jsonBuilder.ToString();
        }

        private static object GetDefaultValue(this Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;

        public static Color HueAdjust(this Color c, float hue)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            return Color.HSVToRGB(hue, s, v);
        }

        public static Color AlphaAdjust(this Color c, float alpha)
        {
            return new Color(c.r, c.g, c.b, alpha);
        }

        public static void MakeEmpty(this Container c)
        {
            c.m_Inspected = true;
            c.m_DisableSerialization = false;
            c.m_RolledSpawnChance = true;
            c.m_NotPopulated = false;
            c.m_StartHasBeenCalled = true;
            c.m_StartInspected = true;
            c.m_GearToInstantiate.Clear();

            if (c.TryGetComponent(out Lock l))
            {
                l.SetLockState(LockState.Unlocked);
                l.m_LockStateRolled = true;
            }
        }
    }
    internal static class Utility
    {
        public const string modVersion = "1.9.4";
        public const string modName = "SafehouseCustomizationPlus";
        public const string modAuthor = "Waltz";

        public const string resourcesFolder = "SafehouseCustomizationPlus.Resources.";
        public const string resourcesFolderForAssumingPlayersUnableToRead = "DumbShield";
        public const string modFolder = "SCPlus/";
        public const string iconsFolder = modFolder + "Icons/";
        public const string iconsCatalog = "catalog_SCPlusIcons";
        public const string placeholderIconName = "placeholder";


        public const string dataSeparator = "_|_";

        public static ModDataManager dataManager = new ModDataManager(modName);
        public static readonly string fireSaveDataTag = "fireBarrels";
        public static readonly string movablesSaveDataTag = "carryables";

        public static PlaceMeshRules genericPlacementRules = PlaceMeshRules.Default | PlaceMeshRules.AllowFloorPlacement | PlaceMeshRules.IgnoreCloseObjects;
        public static PlaceMeshRules boxPlacementRules = PlaceMeshRules.Default | PlaceMeshRules.AllowFloorPlacement | PlaceMeshRules.IgnoreCloseObjects | PlaceMeshRules.AllowStacking;

        public static readonly string missingGuid = "MISSING GUID";

        public static int childrenLookupCoroutineRunning = 0;

        public static readonly Color outlineColor = new Color(0.25f, 0.5f, 1f);


        public enum LayerMask
        {
            Default = 1,
            PossibleDecoration = 787009, // Default(0), Decoration(6), TerrainObject(9), Container(18), InteractiveProp(19)

        }


        public static void Log(ConsoleColor color, string message)
        {
            if (Settings.options.debugLog)
            {
                Melon<SCPMain>.Logger.Msg(color, message);
            }
        }
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

            if (Physics.Raycast(ray, out RaycastHit hit, maxRange, (int)LayerMask.PossibleDecoration))
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

                return hitGo;
            }
            else
            {
                HUDMessage.AddMessage(Localization.Get("SCP_Action_NoValidObject"));
                return null;
            }
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

        public static GameObject GetRealParent(Transform t)
        {
            while (t.parent
                && !WithinDistance(t.parent.position, Vector3.zero)
                && t.parent.name.Contains("_")
                && !t.parent.GetComponent<RadialObjectSpawner>())
            {
                t = t.parent;
            }
            return t.gameObject;
        }

        public static bool TryGetCarryableRoot(Transform t, out GameObject? go)
        {
            while (t.parent
                && !CarryableData.carryablePrefabDefinition.ContainsKey(SanitizeObjectName(t.name)))
            {
                t = t.parent;
            }
            if (CarryableData.carryablePrefabDefinition.ContainsKey(SanitizeObjectName(t.name)))
            {
                go = t.gameObject;
                return true;
            }
            else
            {
                go = null;
                return false;
            }
        }

        public static LocalizedString TryGetLocalizedName(GameObject go)
        {
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
        }

        public static string SanitizeObjectName(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            s = Regex.Replace(s, @"\s|\(.*?\)", ""); // remove spaces, digits and anything within ()
            s = s.Trim(); // remove leading and trailing spaces
            s = Regex.Replace(s, @"\d+$", ""); // remove trailing digits
            return s;
        }

        public static string TryGetGuid(GameObject go)
        {
            ObjectGuid og = GetRealParent(go.transform).GetComponentInChildren<ObjectGuid>();
            return og ? og.GetPDID() : "";
        }

        public static Scene[] GetLoadedScenes()
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

        public static bool FindCarryableAtPosition(string targetName, Vector3 position, float radius, out GameObject? go)
        {
            Collider[] hits = Physics.OverlapSphere(position, radius, (int)LayerMask.PossibleDecoration);

            foreach (var hit in hits)
            {
                ///MelonLogger.Msg(hit.transform.name);
                if (TryGetCarryableRoot(hit.transform, out GameObject? parent))
                {
                    if (SanitizeObjectName(parent.name).ToLower().Contains(targetName.ToLower()))
                    {
                        //MelonLogger.Msg(parent.name + " contains " + targetName);
                        go = parent;
                        return true;
                    }
                }


            }
            go = null;
            return false;
        }





        /*
        [HarmonyPatch(typeof(Il2Cpp.Utils), nameof(Il2Cpp.Utils.ApplyPropertyBlockToRenderers))]
        private static class dfhdfghg
        {
            internal static void Prefix(Il2CppSystem.Collections.Generic.List<Renderer> renderers, MaterialPropertyBlock propertyBlock)
            {
                if (renderers.Count > 0) PrintShaderProperties(renderers[0].material, propertyBlock);
            }
        }


        
        public static void PrintShaderProperties(Material mat, MaterialPropertyBlock mpb)
        {
            Shader shader = mat.shader;
            int count = shader.GetPropertyCount();

            Log(CC.Yellow, $"\nShader: {shader.name}, Property count: {count}");

            for (int i = 0; i < count; i++)
            {
                string name = shader.GetPropertyName(i);
                ShaderPropertyType type = shader.GetPropertyType(i);

                string valueStr = "Unknown";
                string mpbValueStr = "Unknown";

                switch (type)
                {
                    case ShaderPropertyType.Color:
                        valueStr = mat.GetColor(name).ToString();
                        mpbValueStr = mpb.GetColor(name).ToString();
                        break;
                    case ShaderPropertyType.Vector:
                        valueStr = mat.GetVector(name).ToString();
                        mpbValueStr = mpb.GetVector(name).ToString();
                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        valueStr = mat.GetFloat(name).ToString();
                        mpbValueStr = mpb.GetFloat(name).ToString();
                        break;
                    case ShaderPropertyType.Texture:
                        Texture tex = mat.GetTexture(name);
                        Texture mpbtex = mpb.GetTexture(name);
                        valueStr = tex ? tex.name : "None";
                        mpbValueStr = mpbtex ? mpbtex.name : "None";
                        break;
                }

                Log(CC.Gray, $"Property {i}: {name} ({type}) = {valueStr}");
                Log(CC.White, $"    Property {i}: {name} ({type}) = {mpbValueStr}");
            }

            for (int n = -5; n < 400; n++)
            {
                if (mpb.HasColor(n))
                {
                    Log(CC.Green, $"Found Color property #{n}: {mpb.GetColor(n)}");
                }
                if (mpb.HasFloat(n))
                {
                    Log(CC.Green, $"Found Float property #{n}: {mpb.GetFloat(n)}");
                }
                if (mpb.HasVector(n))
                {
                    Log(CC.Green, $"Found Vector property #{n}: {mpb.GetVector(n)}");
                }
                if (mpb.HasBuffer(n))
                {
                    Log(CC.Green, $"Found Buffer property #{n}");
                }
            }
        }
    }*/
    }
}
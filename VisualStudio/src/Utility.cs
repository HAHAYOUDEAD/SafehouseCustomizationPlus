global using CC = System.ConsoleColor;
global using System;
global using MelonLoader;
global using HarmonyLib;
global using UnityEngine;
global using System.Reflection;
global using System.Collections;
global using System.Collections.Generic;
global using Il2Cpp;
global using Il2CppTLD;
global using ModData;
global using Il2CppTLD.Placement;
global using MelonLoader.TinyJSON;
global using LocalizationUtilities;
using Il2CppTLD.Interactions;
using System.Text.RegularExpressions;

namespace SCPlus
{
    internal static class Utility
    {
        public const string modVersion = "1.0";
        public const string modName = "SafehouseCustomizationPlus";
        public const string modAuthor = "Waltz";

        public const string resourcesFolder = "SafehouseCustomizationPlus.Resources.";

        public static ModDataManager dataManager = new ModDataManager(modName);
        public static readonly string fireSaveDataTag = "fireBarrels";




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

        public static GameObject GetRealGameObjectUnderCrosshair()
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

            MemoryStream memoryStream;
            Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (stream != null)
            {
                StreamReader reader = new StreamReader(stream);
                result = reader.ReadToEnd();
            }

            return result;
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
            string name = SanitizeObjectName(go.name);
            if (Localization.Get(name) == name)
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
            s = Regex.Replace(s, @"\s|\(.*?\)", ""); // remove spaces and anything within ()
            return s.Trim();
        }

        public static void Log(ConsoleColor color, string message)
        {
            if (Settings.options.debugLog)
            {
                MelonLogger.Msg(color, message);
            }
        }
        
        public static void RemoveNullsFromCollection<T>(this IEnumerable<T> collection)
        {
            collection.Where(i => i != null);
        }
        public static void RemoveNulls<T>(this List<T> list) where T : new()
        {
            list.RemoveAll(t => t == null);
        }
    }
}

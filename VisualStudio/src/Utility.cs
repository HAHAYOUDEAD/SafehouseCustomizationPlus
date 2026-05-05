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
global using TLD.TinyJSON;
global using ModData;
global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Reflection;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.Diagnostics;
global using UnityEngine;
global using UnityEngine.AddressableAssets;
global using UnityEngine.Events;
global using UnityEngine.SceneManagement;
global using static Il2Cpp.Utils;
global using static SCPlus.Utility;
global using static SCPlus.DecorationHelper;
global using AssetHelper = Il2CppTLD.AddressableAssets.AssetHelper;
global using CC = System.ConsoleColor;
global using CS = SCPlus.CarryableData.CarryableState;
global using CT = SCPlus.CarryableData.CarryableType;
global using SceneManager = UnityEngine.SceneManagement.SceneManager;
using System.IO.Compression;
using System.Security.Cryptography;
using MelonLoader.Utils;

namespace SCPlus
{
    internal static class Utility
    {
        public const string modVersion = "1.9.6";
        public const string modName = "SafehouseCustomizationPlus";
        public const string modAuthor = "Waltz";

        public static string modsPath = MelonEnvironment.ModsDirectory;

        public const string resourcesFolder = "SafehouseCustomizationPlus.Resources.";
        public const string resourcesFolderForAssumingPlayersUnableToRead = "DumbShield";
        public const string resourcesFolderForBreakDown = "BreakDownData";
        public const string modFolder = "SCPlus/";
        public const string iconsFolder = modFolder + "Icons/";
        public const string iconsCatalog = "catalog_SCPlusIcons";
        public const string placeholderIconName = "placeholder";
        public const string missingGuid = "MISSING GUID";

        public const string dataSeparator = "_|_";

        public static ModDataManager dataManager = new ModDataManager(modName);
        public const string movablesSaveDataTag = "carryables";

        public const string travoisName = "INTERACTIVE_Travois";

        public static readonly Color outlineColor = new Color(0.25f, 0.5f, 1f);

        public static Shader standardShader = Shader.Find("Shader Forge/TLD_StandardDiffuse");
        public static Shader transparentShader = Shader.Find("Shader Forge/TLD_StandardTransparent");

        public static void Log(CC color, string message)
        {
            if (Settings.options.debugLog)
            {
                Melon<SCPMain>.Logger.Msg(color, message);
            }
        }
        public static bool GetKeyDown(KeyCode key) => InputManager.HasContext(InputManager.m_CurrentContext) && Input.GetKeyDown(key);
        public static bool GetKeyUp(KeyCode key) => InputManager.HasContext(InputManager.m_CurrentContext) && Input.GetKeyUp(key);
        public static bool GetKeyHeld(KeyCode key) => InputManager.HasContext(InputManager.m_CurrentContext) && Input.GetKey(key);

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

            if (Physics.Raycast(ray, out RaycastHit hit, maxRange, (int)DecoLayerMask.PossibleDecoration))
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

        public static Guid GenerateGuid(int seed)
        {
            var r = new System.Random(seed);
            var guid = new byte[16];
            r.NextBytes(guid);

            return new Guid(guid);
        }

        public static Guid GenerateGuid(string input) // GPT
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                return new Guid(hash);
            }
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

        public static bool IsWithinDistance(Vector3 A, Vector3 B, float distance = 0.01f)
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
                && !IsWithinDistance(t.parent.position, Vector3.zero)
                && t.parent.name.Contains("_")
                && !t.parent.GetComponent<RadialObjectSpawner>())
            {
                t = t.parent;
            }
            return t.gameObject;
        }

        public static string SanitizeObjectName(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            s = Regex.Replace(s, @"\s|\(.*?\)", ""); // remove spaces, digits and anything within ()
            s = s.Trim(); // remove leading and trailing spaces
            s = Regex.Replace(s, @"\d+$", ""); // remove trailing digits
            return s;
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

        public static T[] FindInPlayableScenes<T>(bool includeInactive) where T : UnityEngine.Object // GPT
        {
            return UnityEngine.Object.FindObjectsOfType<T>(includeInactive)
                .Where(obj => obj is Component c
                    ? c.gameObject.scene.name != "DontDestroyOnLoad" && c.gameObject.scene.name != "HideAndDontSave"
                    : (obj as GameObject)?.scene.name != "DontDestroyOnLoad" && (obj as GameObject)?.scene.name != "HideAndDontSave")
                .ToArray();
        }

        public static bool IsInTravois(Transform t)
        {
            while (t.parent)
            {
                if (t.parent.name.Contains(travoisName)) return true;
                t = t.parent;
            }
            return false;
        }

        public static void DialogueSay(string locID, float duration) => InterfaceManager.GetPanel<Panel_Subtitles>().ShowSubtitles(Localization.Get(locID), duration);

        public static void DisplayInteractionButtons(bool show, string primaryLocID = "", string secondaryLocID = "", string tretiaryLocID = "")
        {
            var hud = InterfaceManager.GetPanel<Panel_HUD>().m_GenericInteractionPrompt;
            if (!show)
            {
                hud.HideInteraction();
                return;
            }
            else if (hud.IsShowing())
            {
                return;
            }

            if (IsGamepadActive())
            {
                if (!string.IsNullOrEmpty(primaryLocID)) hud.ShowInteraction(Localization.Get(primaryLocID), "Fire", true);
                if (!string.IsNullOrEmpty(secondaryLocID)) hud.ShowInteraction(Localization.Get(secondaryLocID), "AltFire", true);
                if (!string.IsNullOrEmpty(tretiaryLocID)) hud.ShowInteraction(Localization.Get(tretiaryLocID), "Inventory", true);
            }
            else
            {
                if (!string.IsNullOrEmpty(primaryLocID)) hud.ShowInteraction(Localization.Get(primaryLocID), "Interact", true);
                if (!string.IsNullOrEmpty(secondaryLocID)) hud.ShowInteraction(Localization.Get(secondaryLocID), "AltFire", true);
                if (!string.IsNullOrEmpty(tretiaryLocID)) hud.ShowInteraction(Localization.Get(tretiaryLocID), "Scroll", true);
            }
        }

        public static void DisplayShiftButton(bool enable)
        {
            var hud = InterfaceManager.GetPanel<Panel_HUD>().m_GenericInteractionPrompt;

            hud.HideInteraction();

            if (enable)
            {
                hud.ShowInteraction(Localization.Get("SCP_Action_LiftRestrictions"), "Sprint", true);
                var button = hud.m_SpawnedPrompts.ToArray().FirstOrDefault(p => p.m_InputActionName == "Sprint");
                if (button != null)
                {
                    button.m_KeyboardButtonSprite.width = 80;
                    button.m_KeyboardActionLabel.width = 125;
                    button.transform.localPosition = new Vector3(565f, -90f, 0f);
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using Il2CppTLD.AddressableAssets;
using Il2CppTLD.Scenes;

namespace SCPlus
{
    internal class ConsoleCommands
    {


        [HarmonyPatch(typeof(ConsoleManager), "Initialize")]
        private static class AddCommands
        {
            internal static void Postfix()
            {
                uConsole.RegisterCommand("decoration_spawn", new Action(CONSOLE_TrySpawnDecoration));
                uConsole.RegisterCommand("decoration_search", new Action(CONSOLE_SearchDecoration));
                uConsole.RegisterCommand("decoration_list_repopulate", new Action(()=>MelonCoroutines.Start(CONSOLE_PopulateDecortionsListEnum())));
            }
        }


        public static Dictionary<string, AssetReference> allVanillaDecorations = new(StringComparer.InvariantCultureIgnoreCase);

        public static void CONSOLE_TrySpawnDecoration()
        {
            string name = uConsole.GetString();
            if (string.IsNullOrEmpty(name))
            {
                uConsoleLog.Add("Specify Decoration name");
                return;
            }

            //TemporaryPrefabAsset

            GameObject? go = allVanillaDecorations.ContainsKey(name) ? AssetHelper.SafeInstantiateAssetAsync(allVanillaDecorations[name].m_AssetGUID).WaitForCompletion() : null;
            //allVanillaDecorations[name].LoadAssetAsync<GameObject>().WaitForCompletion() : null;
            //Il2CppTLD.AddressableAssets.AssetHelper.SafeLoadAssetAsync<GameObject>(name).WaitForCompletion();
            ;
            if (go == null)
            {
                uConsoleLog.Add($"Could not load {name}");
                return;
            }
            //go = GameObject.Instantiate(go);

            GameManager.GetPlayerManagerComponent().StartPlaceMesh(go, PlaceMeshFlags.DestroyOnCancel, PlaceMeshRules.None);
            uConsole.TurnOff();

        }

        public static void CONSOLE_SearchDecoration()
        {
            string name = uConsole.GetString();
            if (string.IsNullOrEmpty(name))
            {
                uConsoleLog.Add("Specify Decoration name");
                return;
            }
            List<string> found = new ();
            foreach (var entry in allVanillaDecorations)
            {
                if (entry.Key.ToLowerInvariant().Contains(name)) found.Add(entry.Key);
            }
            if (found.Count > 0)
            {
                uConsoleLog.Add("   Matching Names:");
                foreach (string s in found)
                {
                    uConsoleLog.Add(s);
                }
            }

        }

        public static IEnumerator CONSOLE_PopulateDecortionsListEnum()
        {
            uConsoleLog.Add("Populating...");
            allVanillaDecorations.Clear();
            int i = 0;
            foreach (AssetReference ar in DecorationItemVerificationList.Load().m_DecorationPrefabs)
            {
                i++;

                if (!ar.RuntimeKeyIsValid())
                {
                    continue;
                }
                GameObject go = AssetHelper.SafeLoadAssetAsync<GameObject>(ar.m_AssetGUID).WaitForCompletion();//ar.LoadAssetAsync<GameObject>().WaitForCompletion();
                if (!go)
                {
                    continue;
                }
                string name = go.name;
                while (allVanillaDecorations.ContainsKey(name))
                {
                    name = name.Replace("_Prefab", "") + "_Alt";
                }
                allVanillaDecorations[name] = ar;
                
                if (i > 30)
                {
                    i = 0;
                    yield return new WaitForEndOfFrame();
                }
            }

            uConsoleLog.Add($"Done with {allVanillaDecorations.Count} decorations");
            uConsoleCommandParameterSet? ccps = null;

            foreach (uConsoleCommandParameterSet set in uConsoleAutoComplete.m_CommandParameterSets)
            {
                if (set.m_Commands.Contains("decoration_spawn"))
                {
                    set.m_AllowedParameters.Clear();
                    ccps = set;
                }
            }

            if (ccps == null)
            {
                ccps = new uConsoleCommandParameterSet() { m_Commands = new(), m_AllowedParameters = new () };
                ccps.m_Commands.Add("decoration_spawn");
            }
            
            foreach (var entry in allVanillaDecorations)
            {
                if (!ccps.m_AllowedParameters.Contains(entry.Key))
                {
                    ccps.m_AllowedParameters.Add(entry.Key);
                    ccps.m_AllowedParameters.Add(entry.Key.ToLowerInvariant());
                }
            }
            uConsoleAutoComplete.m_CommandParameterSets.Add(ccps);
            yield break;
        }
    }
}

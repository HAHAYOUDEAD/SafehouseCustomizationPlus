using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCPlus
{
    internal class ContainerPatches
    {

        [HarmonyPatch(typeof(Panel_Container), nameof(Panel_Container.OnInventoryToContainer))]
        private static class PreventStoringCarryablesInCarryables
        {
            internal static bool Prefix(ref Panel_Container __instance)
            {
                var item = __instance.GetCurrentlySelectedItem();

                if (!item.m_DecorationItem)
                {
                    return true;
                }

                DecorationItem di = item.m_DecorationItem;

                SCPlusCarryable scItem = di.GetComponent<SCPlusCarryable>();
                SCPlusCarryable scContainer = __instance.m_Container.GetComponentInParent<SCPlusCarryable>();

                if (scItem && scContainer)
                {
                    GameAudioManager.PlayGUIError();
                    HUDMessage.AddMessage(Localization.Get("SCP_Action_CantStoreCarryableInCarryable"), false, true);
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Panel_Container), nameof(Panel_Container.OnContainerToInventory))]
        private static class PreventTakingUnfinishedDecorations
        {
            internal static bool Prefix(ref Panel_Container __instance)
            {
                var item = __instance.GetCurrentlySelectedItem();

                if (!item.m_DecorationItem)
                {
                    return true;
                }

                if (item.m_DecorationItem.GetComponent<InProgressCraftItem>())
                {
                    GameAudioManager.PlayGUIError();
                    HUDMessage.AddMessage(Localization.Get("Gameplay_DecorationNotAllowedInInventory"), false, true);
                    return false;
                }

                return true;
            }
        }

        /*
        [HarmonyPatch(typeof(ContainerManager), nameof(ContainerManager.FindContainerByGuid))]
        public static class uhhh
        {
            internal static void Postfix(ContainerManager __instance, Container __result, string guid)
            {
                if (__result == null) return;
                if (!IsInTravois(__result.transform))
                {
                    // entering dangerous territory


                    // if container is in DDOL - assign it a new guid
                    if (__result.gameObject.scene.name == "DontDestroyOnLoad")
                    {
                        MelonLogger.Msg(CC.Red, $"1 Found container in inventory: {guid} | {__result.name}");
                        __result = null;
                    }

                }
            }
        }
        

        [HarmonyPatch(typeof(ContainerManager), nameof(ContainerManager.FindContainerByPosition))]
        public static class uhhhh
        {
            internal static void Postfix(ContainerManager __instance, Container __result, ContainerSaveData csd)
            {
                if (__result == null) return;
                if (!IsInTravois(__result.transform))
                {
                    // entering dangerous territory


                    // if container is in DDOL - assign it a new guid
                    if (__result.gameObject.scene.name == "DontDestroyOnLoad")
                    {
                        MelonLogger.Msg(CC.Red, $"2 Found container in inventory: {csd.m_Guid} | {__result.name}");
                        //__result = null;
                    }

                }
            }
        }
        */

        [HarmonyPatch(typeof(Container), nameof(Container.Deserialize))]
        public static class InitDecoInContainers
        {
            public static HashSet<DecorationItem> earlyTravoisList = [];
            public static HashSet<DecorationItem> earlyCarriedTravoisList = [];
            internal static void Prefix(Container __instance, string text)
            {
                if (!IsInTravois(__instance.transform))
                {
                    earlyTravoisList.Clear();
                    earlyCarriedTravoisList.Clear();
                    
                }
            }
            internal static void Postfix(Container __instance)
            {
                foreach (var deco in __instance.m_DecorationItems)
                {
                    if (deco)
                    {
                        if (deco.IconReference == null || !deco.IconReference.RuntimeKeyIsValid()) // icon not set
                        {
                            RelevantSetupForDecorationItem(deco);
                        }
                        else
                        {
                            AdjustDecorationWeight(deco);
                        }

                        BreakDown bd = __instance.GetComponentInChildren<BreakDown>();

                        if (!deco.m_AllowInInventory)
                        {
                            deco.m_AllowInInventory = true;

                            RelevantSetupForDecorationItem(deco, true);

                            if (bd)
                            {
                                bd.m_AllowEditModePlacement = true;
                            }
                        }
                        else
                        {
                            AdjustDecorationWeight(deco);
                        }

                    }
                }

                // readd carryables for late travois initialization
                if (earlyTravoisList.Count > 0)
                {
                    foreach (var d in earlyTravoisList)
                    {
                        __instance.AddDecorationItem(d);
                    }
                }
                if (earlyCarriedTravoisList.Count > 0)
                {
                    foreach (var d in earlyCarriedTravoisList)
                    {
                        __instance.AddDecorationItem(d);
                    }
                }
            }
        }

    }
}

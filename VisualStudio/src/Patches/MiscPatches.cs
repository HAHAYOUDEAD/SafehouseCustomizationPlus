using Il2Cpp;
using Il2CppTLD.BigCarry;
using Il2CppTLD.UI.Scroll;
using static Il2Cpp.Utils;

namespace SCPlus
{
    internal class MiscPatches
    {
        [HarmonyPatch(typeof(Il2Cpp.Utils), nameof(Il2Cpp.Utils.ApplyPropertyBlockToRenderers))]
        public static class SkipOutline
        {
            public static List<int> inProximity = new();
            internal static bool Prefix(Il2CppSystem.Collections.Generic.List<Renderer> renderers, ref MaterialPropertyBlock propertyBlock)
            {
                if (Settings.options.outlineVisibility == 1 && propertyBlock != null) // only when proximity based outlines, ignore outline removal
                {
                    int instanceID = renderers[0].GetInstanceID();

                    if (!inProximity.Contains(instanceID)) // not within proximity
                    {
                        if (!propertyBlock.HasColor("_Color")) // just outline, not highlight (player is not looking at object)
                        {
                            propertyBlock = null; // remove outline
                        }
                    }
                }
                if (Settings.options.outlineVisibility == 3 && propertyBlock != null) // when disabled
                {
                    return false;
                }
                return true;
            }
        }        

        [HarmonyPatch(typeof(Container), nameof(Container.Deserialize))]
        public static class InitDecoInContainers
        {
            public static HashSet<DecorationItem> earlyTravoisList = [];
            public static HashSet<DecorationItem> earlyCarriedTravoisList = [];
            internal static void Prefix(Container __instance)
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
                        BreakDown bd = __instance.GetComponentInChildren<BreakDown>();

                        if (!deco.m_AllowInInventory)
                        {
                            deco.m_AllowInInventory = true;

                            SCPMain.RelevantSetupForDecorationItem(deco, true);

                            if (bd)
                            {
                                bd.m_AllowEditModePlacement = true;
                            }
                        }
                    }
                }

                // readd carryables for late travois initialization
                if (earlyTravoisList.Count > 0) 
                {
                    foreach(var d in earlyTravoisList)
                    {
                        __instance.AddDecorationItem(d);
                    }
                }                
                if (earlyCarriedTravoisList.Count > 0)
                {
                    foreach(var d in earlyCarriedTravoisList)
                    {
                        __instance.AddDecorationItem(d);
                    }
                }
            }
        }
   
        [HarmonyPatch(typeof(InteractiveLightsource), nameof(InteractiveLightsource.Awake))]
        private static class TunnelLanternFix
        {
            internal static void Postfix(InteractiveLightsource __instance)
            {
                __instance.gameObject.layer = vp_Layer.InteractiveProp; // doesn't affect anything, just sets the layer earlier than the game
            }
        }   

        [HarmonyPatch(typeof(InteractiveLightsource), nameof(InteractiveLightsource.PerformInteraction))]
        private static class TunnelLanternShowFuel
        {
            internal static void Postfix(InteractiveLightsource __instance)
            {
                if (__instance.TryGetComponent<SCPlusSimpleFuelTank>(out var ft))
                {
                    InterfaceManager.GetPanel<Panel_Subtitles>().ShowSubtitles(ft.GetRemainingFuelTimeProcessedString(), 8f);
                }
            }
        }

        [HarmonyPatch(typeof(BigCarryItem), nameof(BigCarryItem.PerformInteraction))]
        private static class NoTravoisWhenCustomizing
        {
            internal static bool Prefix(ref BigCarryItem __instance, ref bool __result)
            {
                if (GameManager.GetSafehouseManager().IsCustomizing() && __instance.name.Contains(travoisName))
                {
                    GameAudioManager.PlayGUIError();
                    HUDMessage.AddMessage(Localization.Get("SCP_Action_NoTravoisWhenCustomizing"), true, true);
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Panel_HUD), nameof(Panel_HUD.UpdateSafehouse))] // hide SC icons
        private static class HideIconWhenNotCustomizing
        {
            internal static void Postfix(ref Panel_HUD __instance)
            {
                if (Settings.options.hideIcon > 0)
                {
                    if (Settings.options.hideIcon == 1 && !GameManager.GetSafehouseManager().IsCustomizing()) // only hide when not ccustomizing
                    {
                        __instance.m_SafehouseRoot.active = false;
                    }
                    if (Settings.options.hideIcon == 2) // always hide
                    {
                        __instance.m_SafehouseRoot.active = false;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetExtraWeightKG))] // 
        private static class AddDecorationToOverallCarriedWeight
        {
            internal static void Postfix(ref ItemWeight __result)
            {
                if (GameManager.GetPlayerManagerComponent().IsInMeshPlacementMode() && GameManager.GetPlayerManagerComponent().m_ObjectToPlaceDecorationItem)
                {
                    __result = ItemWeight.FromKilograms(__result.ToQuantity(1f) + CarryableData.carriedObjectWeight);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGridItem), nameof(InventoryGridItem.RefreshDecorationItem))]
        private static class RefreshInventoryGridIndicationIcon
        {
            internal static void Postfix(ref InventoryGridItem __instance)
            {
                DecorationItem di = __instance.GetDecorationItem();

                if (di)
                {
                    if (SCPMain.iconGuidLookupList.Contains(di.IconReference.RuntimeKey.ToString()))
                    {
                        __instance.m_DecorationIndicator.GetComponent<UISprite>().spriteName = "ico_SafehouseCustomization";
                        //InterfaceManager.GetPanel<Panel_Inventory>().m_ItemDescriptionPage.m_ItemNotesLabel.
                        return;
                    }
                    __instance.m_DecorationIndicator.GetComponent<UISprite>().spriteName = "ico_Safehouse";
                }
            }
        }

        [HarmonyPatch(typeof(ItemDescriptionPage), nameof(ItemDescriptionPage.UpdateDecorationItemDescription))]
        private static class ChangeCarryableInventoryLabel
        {
            internal static void Postfix(ref ItemDescriptionPage __instance, ref DecorationItem di)
            {
                if (SCPMain.iconGuidLookupList.Contains(di.IconReference.RuntimeKey.ToString()))
                {
                    if (di.GetComponent<SCPlusCarryable>())
                    {
                        __instance.m_ItemNotesLabel.text = Localization.Get("SCP_Label_Carryable");
                        return;
                    }
                    __instance.m_ItemNotesLabel.text = Localization.Get("SCP_Label_Decoration");
                }
            }
        }

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
    }
}
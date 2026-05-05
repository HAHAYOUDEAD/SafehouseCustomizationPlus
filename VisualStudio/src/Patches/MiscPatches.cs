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
                //if (GameManager.GetPlayerManagerComponent().IsInPlacementMode()) return true;

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

        [HarmonyPatch(typeof(InputManager), nameof(InputManager.ExecuteAltFire))] 
        internal class ProcessStructureRightClick
        {
            private static void Postfix()
            {
                if (!SCPMain.isLoaded) return;

                SCPlusSimpleFuelTank? ft = GetInteractiveGameObjectUnderCrosshair()?.GetComponentInChildren<SCPlusSimpleFuelTank>();
                if (ft)
                {
                    ft.Refuel();
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
                    //GameManager.GetPlayerVoiceComponent().Play("Play_FireFail", Il2CppVoice.Priority.Critical); // that didn't work
                    // Play_FailGeneralSwitch // damn it/ come on
                    // Play_VOCatchBreath // phew/exhale
                    // Play_VOInspectObject // could find use/useful
                    // Play_VOInspectObjectImportant // lucky day/perfect


                    DialogueSay(ft.GetRemainingFuelTimeProcessedString(), 8f);
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

        [HarmonyPatch(typeof(Panel_HUD), nameof(Panel_HUD.SetHoverText))]
        public class HideHoverLabels
        {
            public static void Postfix(Panel_HUD __instance, GameObject itemUnderCrosshairs, ref string hoverText)
            {
                if (!Settings.options.disableHoverLabels) return;
                if (!GameManager.GetSafehouseManager() || !GameManager.GetSafehouseManager().IsCustomizing()) return;
                if (itemUnderCrosshairs?.GetComponentInChildren<DecorationItem>())
                {
                    __instance.m_Label_ObjectName.text = "";
                    __instance.m_HoverTextBG.enabled = false;
                    __instance.m_HoverTextLinebreak.enabled = false;
                }
            }
        }

        /*
        [HarmonyPatch(typeof(PdidTable), nameof(PdidTable.RuntimeAddOrReplace))]
        private static class PreventDuplicateGuids
        {
            internal static void Prefix(PdidTable __instance, PdidObjectBase pdidObject, string guid)
            {
                if (pdidObject == null || PdidTable.GetGameObject(guid) == null) return;

                if (pdidObject.gameObject.scene != PdidTable.GetGameObject(guid).scene)
                {
                    MelonLogger.Msg(CC.Red, $"1 DUPLICATE GUID {guid} | {PdidTable.GetGameObject(guid).name}");
                }
            }
        }        
        
        [HarmonyPatch(typeof(PdidTable), nameof(PdidTable.RuntimeRegister))]
        private static class PreventDuplicateGuids2
        {
            internal static void Prefix(PdidTable __instance, PdidObjectBase pdidObject, string guid)
            {
                if (pdidObject == null || PdidTable.GetGameObject(guid) == null) return;

                if (pdidObject.gameObject.scene != PdidTable.GetGameObject(guid).scene)
                {
                    MelonLogger.Msg(CC.Red, $"2 DUPLICATE GUID {guid} | {PdidTable.GetGameObject(guid).name}");
                }
            }
        }
        */
    }
}
namespace SCPlus
{
    internal class BreakDownPatches
    {

        [HarmonyPatch(typeof(ActionPickerItem), nameof(ActionPickerItem.OnClick))]
        private static class EnableBreakdownPanelFromSCpre
        {
            public static bool toggle;
            internal static void Prefix(ref ActionPickerItem __instance)
            {
                if (GameManager.GetSafehouseManager().IsCustomizing() && __instance.m_Sprite.spriteName == "ico_harvest")
                {
                    toggle = true;
                }
            }
        }

        [HarmonyPatch(typeof(Panel_BreakDown), nameof(Panel_BreakDown.ShowRelevantGroups))]
        private static class EnableBreakdownPanelFromSC
        {

            internal static void Prefix()
            {
                if (EnableBreakdownPanelFromSCpre.toggle)
                {
                    GameManager.GetSafehouseManager().m_IsCustomizing = false;
                }
            }
        }

        [HarmonyPatch(typeof(BreakDown), nameof(BreakDown.RequiresTool))]
        private static class ToolReqWhenBreakdownPanelFromSC
        {

            internal static void Postfix(ref BreakDown __instance, ref bool __result)
            {
                if (EnableBreakdownPanelFromSCpre.toggle)
                {
                    __result = __instance.m_RequiresTool;
                }
            }
        }

        [HarmonyPatch(typeof(Panel_BreakDown), nameof(Panel_BreakDown.Enable))]
        private static class NoMoreBreakdownPanelFromSC
        {
            internal static void Prefix(ref bool enable)
            {
                if (enable && EnableBreakdownPanelFromSCpre.toggle)
                {
                    GameManager.GetSafehouseManager().m_IsCustomizing = false;
                }
            }
            internal static void Postfix(ref Panel_BreakDown __instance, ref bool enable)
            {

                if (!enable && EnableBreakdownPanelFromSCpre.toggle)
                {
                    EnableBreakdownPanelFromSCpre.toggle = false;
                    GameManager.GetSafehouseManager().m_IsCustomizing = true;
                }
            }
        }

        [HarmonyPatch(typeof(BreakDown), nameof(BreakDown.PerformInteraction))]
        private static class PreventDestroy
        {
            internal static bool Prefix(ref BreakDown __instance)
            {
                if (__instance.m_EditModeDestroyOnly)
                {
                    WoodStove ws = __instance.GetComponent<WoodStove>();
                    if (ws && ws.Fire.IsBurning())
                    {
                        GameAudioManager.PlayGUIError();
                        HUDMessage.AddMessage(Localization.Get("SCP_Action_CantDestroyHot"), false, true);
                        __instance.GetComponent<DecorationItem>()?.m_ActionPicker.TrySetEnabled(false);
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(BreakDown), nameof(BreakDown.Awake))]
        private static class EnableBreakdown
        {
            internal static void Prefix(ref BreakDown __instance)
            {
                if (!__instance.isActiveAndEnabled)
                {
                    if (__instance.m_YieldObject != null && __instance.m_YieldObject.Length > 0)
                    {
                        __instance.enabled = true;
                    }
                    else
                    {
                        Log(CC.Gray, $"BreakDown exists but doesn't have yields: {__instance.name}");
                    }
                }
            }
            internal static void Postfix(ref BreakDown __instance)
            {
                if (__instance.gameObject.layer == vp_Layer.Default)
                {
                    __instance.gameObject.layer = vp_Layer.InteractiveProp;
                }
            }
        }
    }
}

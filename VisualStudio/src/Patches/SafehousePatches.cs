using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCPlus
{
    internal class SafehousePatches
    {
        [HarmonyPatch(typeof(SafehouseManager), nameof(SafehouseManager.Awake))]
        private static class OutlineThings
        {
            internal static void Postfix(ref SafehouseManager __instance)
            {
                __instance.m_OutlineColor = outlineColor.HueAdjust(Settings.options.outlineHue).AlphaAdjust(Settings.options.outlineAlpha);
                __instance.m_OnHoverColor = outlineColor.HueAdjust(Settings.options.outlineHue);

                __instance.m_OnHoverPropertyBlock.SetColor("_Color", __instance.m_OnHoverColor);

                __instance.m_OutlineThickness = Settings.options.outlineThickness;
            }
        }

        [HarmonyPatch(typeof(SafehouseManager), nameof(SafehouseManager.StartCustomizing))]
        private static class AddBall
        {
            internal static void Postfix()
            {
                if (Settings.options.outlineVisibility == 1)
                {
                    GameManager.GetPlayerTransform().gameObject.GetOrAddComponent<SCPlusDecorationDetector>();
                }
            }
        }

        [HarmonyPatch(typeof(SafehouseManager), nameof(SafehouseManager.StopCustomizing))]
        private static class RemoveBall
        {
            internal static void Postfix()
            {
                if (Settings.options.outlineVisibility == 1)
                {
                    if (GameManager.GetPlayerTransform().TryGetComponent(out SCPlusDecorationDetector d))
                    {
                        GameObject.Destroy(d);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SafehouseManager), nameof(SafehouseManager.InCustomizableSafehouse))]
        private static class AlwaysCustomizable
        {
            internal static void Postfix(ref SafehouseManager __instance, ref bool __result)
            {
                __result = true; // if (Settings.options.enableCustomizationAnywhere)
            }
        }

        [HarmonyPatch(typeof(SafehouseManager), nameof(SafehouseManager.TryStartCustomizing))]
        private static class NoCustomizationWithTravois
        {
            internal static bool Prefix(ref SafehouseManager __instance, ref bool __result)
            {
                if (GameManager.GetPlayerAnimationComponent().m_Animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.ToLower().Contains("travois"))
                {
                    GameAudioManager.PlayGUIError();
                    HUDMessage.AddMessage(Localization.Get("SCP_Action_NoCustomizationWithTravois"), true, true);
                    __result = false;
                    return false;
                }
                return true;
            }
        }
    }
}
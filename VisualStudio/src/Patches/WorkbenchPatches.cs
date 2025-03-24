namespace SCPlus
{

    internal class WorkbenchPatches
    {

        [HarmonyPatch(typeof(Panel_HUD), nameof(Panel_HUD.SetHoverText))] // workbench hover prompts
        public class ShowButtonPrompts
        {
            public static void Prefix(ref GameObject itemUnderCrosshairs)
            {
                if (!Settings.options.altWorkbenchInteraction) return;
                if (!GameManager.GetSafehouseManager() || GameManager.GetSafehouseManager().IsCustomizing()) return;
                if (itemUnderCrosshairs?.GetComponent<WorkBench>() == null) return;
                if (InterfaceManager.GetPanel<Panel_HUD>()?.m_EquipItemPopup)
                {
                    InterfaceManager.GetPanel<Panel_HUD>().m_EquipItemPopup.enabled = true;
                    InterfaceManager.GetPanel<Panel_HUD>().m_EquipItemPopup.ShowGenericPopupWithDefaultActions(Localization.Get("GAMEPLAY_Crafting"), Localization.Get("GAMEPLAY_BreakDown"));

                }

            }
        }

        [HarmonyPatch(typeof(WorkBench), nameof(WorkBench.InteractWithWorkbench))] // workbench interaction main
        public class WorkbenchPrimaryAction
        {
            public static bool Prefix(ref WorkBench __instance)
            {
                if (!Settings.options.altWorkbenchInteraction) return true;

                if (GameManager.GetSafehouseManager().IsCustomizing()) return true;

                __instance.OnCrafting();

                return false;
            }
        }

        [HarmonyPatch(typeof(InputManager), nameof(InputManager.ExecuteAltFire))] // workbench interaction alt
        public class CatchAltInteractionWithWorkbench
        {
            public static void Prefix()
            {
                if (!Settings.options.altWorkbenchInteraction) return;

                if (!GameManager.GetPlayerManagerComponent()) return;

                if (GameManager.GetSafehouseManager().IsCustomizing()) return;

                GameObject wb = GetInteractiveGameObjectUnderCrosshair();
                if (wb?.GetComponent<WorkBench>() == null) return;
                wb.GetComponent<WorkBench>().OnBreakDown();
            }
        }
    }
}

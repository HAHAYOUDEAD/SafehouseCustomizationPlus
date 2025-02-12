namespace SCPlus
{
    internal class GreenScreenPatches
    {
        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.StartPlaceMesh), [typeof(GameObject), typeof(float), typeof(PlaceMeshFlags), typeof(PlaceMeshRules)])]
        private static class SetupGreenScreen
        {
            internal static bool Prefix(PlayerManager __instance, ref GameObject objectToPlace)
            {
                if (!Settings.options.devInspect || !InputManager.GetSprintDown(InputManager.m_CurrentContext)) return true;

                DecorationItem di = objectToPlace.GetComponent<DecorationItem>();

                if (di) // setting up greenscreen
                {
                    GearItem gi = GearItem.Instantiate(GearItem.LoadGearItemPrefab("GEAR_Stone"));
                    foreach (Renderer r in gi.m_MeshRenderers)
                    {
                        r.enabled = false;
                    }
                    SCPMain.DEVInspectTempGO = GameObject.Instantiate(di.gameObject);

                    SCPMain.DEVInspectTempGO.transform.localScale = Vector3.one * 0.1f;

                    GameManager.GetPlayerManagerComponent().EnterInspectGearMode(gi);
                    SCPMain.DEVInspectTempGO.transform.SetParent(gi.transform);
                    Collider bc = SCPMain.DEVInspectTempGO.GetComponent<Collider>();
                    SCPMain.DEVInspectTempGO.transform.position = Vector3.zero;
                    SCPMain.DEVInspectTempGO.transform.localPosition = Vector3.zero;
                    if (bc) SCPMain.DEVInspectTempGO.transform.localPosition += Vector3.down * bc.bounds.extents.y / 2f;
                    SCPMain.DEVInspectTempGO.transform.localRotation = Quaternion.identity;
                    foreach (Transform t in SCPMain.DEVInspectTempGO.GetComponentsInChildren<Transform>())
                    {
                        t.gameObject.layer = vp_Layer.InspectGear;
                    }

                    InterfaceManager.GetPanel<Panel_HUD>().m_InspectMode_InspectPrompts.active = false;
                    InterfaceManager.GetPanel<Panel_HUD>().m_InspectMode_StatDetails.gameObject.active = false;
                    InterfaceManager.GetPanel<Panel_HUD>().m_InspectMode_Description.gameObject.active = false;
                    InterfaceManager.GetPanel<Panel_HUD>().m_InspectMode_Title.gameObject.active = false;
                    InterfaceManager.GetPanel<Panel_HUD>().m_InspectModeItemTypeIcons[0].transform.GetParent().gameObject.active = false;
                    InterfaceManager.GetPanel<Panel_HUD>().m_InspectMode_InventoryStatusSprite.gameObject.active = false;

                    if (SCPMain.catalogParsed.ContainsKey(SanitizeObjectName(objectToPlace.name)) || (di.IconReference.RuntimeKeyIsValid() && di.IconReference.RuntimeKey.ToString() != SCPMain.catalogParsed[placeholderIconName]))
                    {
                        HUDMessage.AddMessage($"{SanitizeObjectName(objectToPlace.name)} already has an icon!", false, true);
                    }

                    SCPMain.DEVInspectMode = true;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.ExitInspectGearMode))] // restore after greenscreen
        private static class StopDevInspect
        {
            internal static void Prefix(ref PlayerManager __instance)
            {
                if (!Settings.options.devInspect) return;
                if (!__instance.m_InspectModeActive || !SCPMain.DEVInspectMode)
                {
                    return;
                }
                SCPMain.DEVInspectMode = false;
                SCPMain.SetupGreenscreen(GameManager.GetMainCamera(), true);
                if (InterfaceManager.GetPanel<Panel_HUD>().m_InspectMode_StatDetails)
                    InterfaceManager.GetPanel<Panel_HUD>().m_InspectMode_StatDetails.gameObject.active = true;
                if (InterfaceManager.GetPanel<Panel_HUD>().m_InspectModeItemTypeIcons[0]?.transform?.GetParent())
                    InterfaceManager.GetPanel<Panel_HUD>().m_InspectModeItemTypeIcons[0].transform.GetParent().gameObject.active = true;
                if (InterfaceManager.GetPanel<Panel_HUD>().m_InspectMode_InventoryStatusSprite)
                    InterfaceManager.GetPanel<Panel_HUD>().m_InspectMode_InventoryStatusSprite.gameObject.active = true;

                GameObject.Destroy(SCPMain.DEVInspectTempGO?.transform?.parent?.gameObject);
            }
        }


        [HarmonyPatch(typeof(UniStormWeatherSystem), nameof(UniStormWeatherSystem.Update))] // here because camera background changes to black in this method, setup for greenscreen
        private static class DevInspectGreenscreen
        {
            internal static void Postfix()
            {
                if (!SCPMain.DEVInspectMode) return;
                if (GameManager.GetPlayerManagerComponent().IsInspectModeActive())
                {
                    SCPMain.SetupGreenscreen(GameManager.GetMainCamera());
                }
            }
        }

    }
}

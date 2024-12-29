using Il2Cpp;
using Il2CppTLD.IntBackedUnit;
using Unity.VisualScripting;
using UnityEngine;

namespace SCPlus
{
    internal class UnbreakablePatches
    {
        [HarmonyPatch(typeof(SafehouseManager), nameof(SafehouseManager.InCustomizableSafehouse))]
        private static class AlwaysCustomizable
        {
            internal static void Postfix(ref SafehouseManager __instance, ref bool __result)
            {
                if (Settings.options.enableCustomizationAnywhere) __result = true;
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
                    __result = ItemWeight.FromKilograms(__result.ToQuantity(1f) + CarriedObjectData.carriedObjectWeight);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.StartPlaceMesh), [typeof(GameObject), typeof(float), typeof(PlaceMeshFlags), typeof(PlaceMeshRules)])]
        private static class ManagePlacement
        {
            internal static bool Prefix(PlayerManager __instance, ref GameObject objectToPlace)
            {
                if (!Settings.options.pickupAnything && !Settings.options.pickupContainers) return true;

                objectToPlace.TryGetComponent(out DecorationItem di);

                if (di)
                {
                    foreach (Renderer r in di.GetRenderers()) // fix drastic performance drop on some objects
                    {
                        if (r)
                        {
                            r.enabled = true;
                            if (r.name.EndsWith("_Shadow"))
                            {
                                r.gameObject.active = false;
                            }
                        } 
                    }
                    Il2CppSystem.Collections.Generic.List<DecorationItem> children = new();
                    foreach (DecorationItem child in di.DecorationChildren)
                    {
                        if (!child) continue;
                        child.gameObject.active = true;
                        children.Add(child);
                    }
                    di.m_DecorationChildren = children;
                    

                    WoodStove ws = __instance.GetComponent<WoodStove>();
                    if (ws && ws.Fire.IsBurning())
                    {
                        GameAudioManager.PlayGUIError();
                        HUDMessage.AddMessage(Localization.Get("SCP_Action_CantMoveHot"));
                        __instance.CancelPlaceMesh();
                        return false;
                    }

                    bool shouldCalculateWeight = di.gameObject.scene.name != "DontDestroyOnLoad" && !Settings.options.ignorePlaceWeight;

                    Container[] c = objectToPlace.GetComponentsInChildren<Container>();
                    if (c.Length > 0)
                    {
                        CarriedObjectData.carriedObjectWeight = 0f;
                        foreach (Container cc in c)
                        {
                            if (shouldCalculateWeight)
                            {
                                CarriedObjectData.carriedObjectWeight += cc.GetTotalWeightKG().ToQuantity(1f);

                                if (!SCPMain.justDupedContainer) continue;
                            }
                            cc.m_Inspected = true;
                            cc.m_DisableSerialization = false;
                            cc.m_RolledSpawnChance = true;
                            cc.m_NotPopulated = false;
                            cc.m_StartHasBeenCalled = true;
                            cc.m_StartInspected = true;
                            cc.m_GearToInstantiate.Clear();
                            cc.TryGetComponent(out Lock l);
                            if (l)
                            {
                                l.SetLockState(LockState.Unlocked);
                                l.m_LockStateRolled = true;
                            }
                        }
                        if (shouldCalculateWeight)
                        {
                            CarriedObjectData.carriedObjectWeight += di.Weight.ToQuantity(1f);
                            Log(CC.Gray, "Moving in-scene container, weight: " + CarriedObjectData.carriedObjectWeight);
                            float totalCarriedWeight = CarriedObjectData.carriedObjectWeight + GameManager.GetEncumberComponent().GetGearWeightKG().ToQuantity(1f);
                            if (totalCarriedWeight > GameManager.GetEncumberComponent().GetNoWalkCarryCapacityKG().ToQuantity(1f))
                            {
                                GameAudioManager.PlayGUIError();
                                HUDMessage.AddMessage(Localization.Get("SCP_Action_CantMoveHeavy"));
                                SCPMain.justDupedContainer = false;
                                __instance.CancelPlaceMesh();
                                return false;
                            }
                        }
                    }
                    else if (shouldCalculateWeight)
                    {
                        CarriedObjectData.carriedObjectWeight = di.Weight.ToQuantity(1f);
                        float totalCarriedWeight = CarriedObjectData.carriedObjectWeight + GameManager.GetEncumberComponent().GetGearWeightKG().ToQuantity(1f);
                        if (totalCarriedWeight > GameManager.GetEncumberComponent().GetNoWalkCarryCapacityKG().ToQuantity(1f))
                        {
                            GameAudioManager.PlayGUIError();
                            HUDMessage.AddMessage(Localization.Get("SCP_Action_CantMoveHeavy"));
                            __instance.CancelPlaceMesh();
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.ExitMeshPlacement))]
        private static class ExitPlacement
        {
            static DecorationItem di;
            internal static void Prefix(ref PlayerManager __instance)
            {
                di = __instance.m_ObjectToPlaceDecorationItem;
            }

            internal static void Postfix()
            {
                SCPMain.justDupedContainer = false;

                if (di?.isActiveAndEnabled == true)
                {
                    foreach (Renderer r in di.GetRenderers()) // reset shadow mesh
                    {
                        if (r)
                        {
                            if (r.name.EndsWith("_Shadow"))
                            {
                                r.gameObject.active = true;
                            }
                        }
                    }
                }
            }
        }
    }
}
 
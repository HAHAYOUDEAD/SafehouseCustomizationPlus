using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCPlus
{
    internal class PlayerManagerPatches
    {
        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.StartPlaceMesh), [typeof(GameObject), typeof(float), typeof(PlaceMeshFlags), typeof(PlaceMeshRules)])]
        private static class ManagePlacement
        {
            public static Vector3 offset = Vector3.zero;
            public static HashSet<Collider> disabledCollidersForPlacement = [];

            internal static bool Prefix(PlayerManager __instance, ref GameObject objectToPlace, ref bool __result)
            {
                //if (!Settings.options.pickupAnything && !Settings.options.pickupContainers) return true;

                objectToPlace.TryGetComponent(out DecorationItem di);

                if (!di) // for non-vanilla decorations spawned with console
                {
                    foreach (var entry in CarryableData.carryablePrefabDefinition)
                    {
                        if (objectToPlace.name.ToLower().Contains(entry.Key.ToLower()))
                        {
                            di = MakeIntoDecoration(objectToPlace, entry.Value.placeRules);
                        }
                    }
                }

                if (di)
                {
                    string name = SanitizeObjectName(di.name);

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

                    WoodStove ws = objectToPlace.GetComponentInChildren<WoodStove>(true);
                    if (ws && ws.Fire?.IsBurning() == true)
                    {
                        GameAudioManager.PlayGUIError();
                        HUDMessage.AddMessage(Localization.Get("SCP_Action_CantMoveHot"));
                        __instance.CancelPlaceMesh();
                        __result = false;
                        return false;
                    }

                    bool shouldCalculateWeight = di.gameObject.scene.name != "DontDestroyOnLoad" && !Settings.options.ignorePlaceWeight && !decorationJustDuped;
                    bool isFromInventory = di.gameObject.scene.name == "DontDestroyOnLoad" || GameManager.GetInventoryComponent().m_DecorationItems.Contains(di);

                    Container[] c = objectToPlace.GetComponentsInChildren<Container>();

                    if (c.Length > 0)
                    {
                        CarryableData.carriedObjectWeight = 0f;
                        foreach (Container cc in c)
                        {
                            if (shouldCalculateWeight)
                            {
                                CarryableData.carriedObjectWeight += cc.GetTotalWeightKG().ToQuantity(1f);
                            }

                            if (decorationJustDuped || isFromInventory)
                            {
                                cc.MakeEmpty();
                            }

                        }
                        if (shouldCalculateWeight)
                        {
                            CarryableData.carriedObjectWeight += di.Weight.ToQuantity(1f);
                            Log(CC.Gray, "Moving in-scene container, weight: " + CarryableData.carriedObjectWeight);
                            float totalCarriedWeight = CarryableData.carriedObjectWeight + GameManager.GetEncumberComponent().GetGearWeightKG().ToQuantity(1f);
                            if (totalCarriedWeight > GameManager.GetEncumberComponent().GetNoWalkCarryCapacityKG().ToQuantity(1f))
                            {
                                GameAudioManager.PlayGUIError();
                                HUDMessage.AddMessage(Localization.Get("SCP_Action_CantMoveHeavy"));
                                //SCPMain.decorationJustDuped = false;
                                __instance.CancelPlaceMesh();
                                __result = false;
                                return false;
                            }
                        }
                    }

                    else if (shouldCalculateWeight)
                    {
                        CarryableData.carriedObjectWeight = di.Weight.ToQuantity(1f);
                        float totalCarriedWeight = CarryableData.carriedObjectWeight + GameManager.GetEncumberComponent().GetGearWeightKG().ToQuantity(1f);
                        if (totalCarriedWeight > GameManager.GetEncumberComponent().GetNoWalkCarryCapacityKG().ToQuantity(1f))
                        {
                            GameAudioManager.PlayGUIError();
                            HUDMessage.AddMessage(Localization.Get("SCP_Action_CantMoveHeavy"));
                            //SCPMain.decorationJustDuped = false;
                            __instance.CancelPlaceMesh();
                            __result = false;
                            return false;
                        }
                    }

                    SCPlusCarryable? carryable = CarryableData.SetupCarryable(di, false); // listed in exitmeshplacement

                    if (carryable != null)
                    {
                        if (decorationJustDuped)
                        {
                            carryable.isInstance = true;
                        }
                        carryable.RetrieveAdditionalData();

                        if (CarryableData.TryGetOrAddFireGuid(di.gameObject, out var og))
                        {
                            if (string.IsNullOrEmpty(og?.GetPDID()))
                            {
                                PdidTable.RuntimeAddOrReplace(og, Guid.NewGuid().ToString());
                            }
                            Log(CC.Gray, $"Adding Fire GUID {og.GetPDID()} to {name}");

                        }
                    }

                    if (di.name.ToLower().Contains("curtain"))
                    {
                        di.name = name;
                    }

                    offset = Vector3.zero;

                    if (CarryableData.decorationOverrideData.TryGetValue(name, out OverrideData? od))
                    {
                        if (od.placementOffset != Vector3.zero)
                        {
                            offset = od.placementOffset;
                        }
                    }

                    var colliders = di.GetComponentsInChildren<Collider>();

                    if (colliders != null && colliders.Length > 1)
                    {
                        Collider mainCollider = null;

                        foreach (var anyCol in colliders)
                        {
                            if (anyCol != null && anyCol.enabled == true)
                            {
                                disabledCollidersForPlacement.Add(anyCol);
                                anyCol.enabled = false;

                                if (mainCollider == null || anyCol.bounds.GetVolumeCubic() > mainCollider.bounds.GetVolumeCubic())
                                {
                                    mainCollider = anyCol;
                                }
                            }
                        }

                        mainCollider.enabled = true;
                        Log(CC.Gray, "Disabled " + (colliders.Length - 1) + " colliders for placement, main collider: " + mainCollider.name);
                    }
                }

                DisplayShiftButton(true);
                return true;
            }
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.UpdatePlaceMesh))]
        private static class PlacementOffset
        {
            internal static void Postfix(ref PlayerManager __instance)
            {
                if (ManagePlacement.offset != Vector3.zero && __instance.IsInMeshPlacementMode() && __instance.m_ObjectToPlace != null)
                {
                    __instance.m_ObjectToPlace.transform.position += ManagePlacement.offset;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.AttemptToPlaceMesh))]
        private static class ResetOffset
        {
            internal static void Prefix(ref PlayerManager __instance)
            {
                if (__instance.CanPlaceCurrentPlaceable())
                {
                    ManagePlacement.offset = Vector3.zero;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.DoPositionCheck))]
        private static class OverridePositionCheck
        {
            internal static void Postfix(ref PlayerManager __instance, ref MeshLocationCategory __result)
            {
                if (!__instance.m_ObjectToPlaceDecorationItem) return;

                if (InputManager.GetSprintDown(InputManager.m_CurrentContext) &&
                   (__result != MeshLocationCategory.Valid ||
                    __result != MeshLocationCategory.ValidOutOfRange ||
                    __result != MeshLocationCategory.ValidOutOfRangeFar))
                {
                    float distance = Vector3.Distance(__instance.m_ObjectToPlace.transform.position, GameManager.GetPlayerTransform().position);
                    if (distance > __instance.m_PlacementDistanceFar)
                    {
                        __result = MeshLocationCategory.ValidOutOfRangeFar;
                        return;
                    }
                    else if (distance > __instance.m_PlacementDistance)
                    {
                        __result = MeshLocationCategory.ValidOutOfRange;
                        return;
                    }

                    __result = MeshLocationCategory.Valid;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.ExitMeshPlacement))]
        private static class ExitPlacement
        {
            static DecorationItem di;
            internal static void Prefix(ref PlayerManager __instance)
            {
                di = __instance.m_ObjectToPlaceDecorationItem;
                foreach (var c in ManagePlacement.disabledCollidersForPlacement)
                {
                    if (c) c.enabled = true;
                }
                ManagePlacement.disabledCollidersForPlacement.Clear();
                //ManagePlacement.offset = Vector3.zero;
            }
            internal static void Postfix()
            {
                decorationJustDuped = false;

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
                    di.TryGetComponent(out SCPlusCarryable carryable);
                    if (carryable) CarryableManager.Add(carryable);
                }

                DisplayShiftButton(false);
            }
        }
    }
}

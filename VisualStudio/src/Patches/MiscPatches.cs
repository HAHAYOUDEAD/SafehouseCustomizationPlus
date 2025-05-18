using Il2Cpp;
using Il2CppTLD.BigCarry;
using Il2CppTLD.UI.Scroll;

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
        private static class InitDecoInContainers
        {
            internal static void Postfix(ref Container __instance)
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
            }
        }

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

        [HarmonyPatch(typeof(BigCarryItem), nameof(BigCarryItem.PerformInteraction))]
        private static class NoTravoisWhenCustomizing
        {
            internal static bool Prefix(ref BigCarryItem __instance, ref bool __result)
            {
                if (GameManager.GetSafehouseManager().IsCustomizing() && __instance.name.ToLower().Contains("travois"))
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

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.StartPlaceMesh), [typeof(GameObject), typeof(float), typeof(PlaceMeshFlags), typeof(PlaceMeshRules)])]
        private static class ManagePlacement
        {
            public static Vector3 offset = Vector3.zero;

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
                            di = SCPMain.MakeIntoDecoration(objectToPlace);
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

                    bool shouldCalculateWeight = di.gameObject.scene.name != "DontDestroyOnLoad" && !Settings.options.ignorePlaceWeight && !SCPMain.decorationJustDuped;
                    
                    Container[] c = objectToPlace.GetComponentsInChildren<Container>();
                    
                    if (c.Length > 0)
                    {
                        CarryableData.carriedObjectWeight = 0f;
                        foreach (Container cc in c)
                        {
                            if (shouldCalculateWeight)
                            {
                                CarryableData.carriedObjectWeight += cc.GetTotalWeightKG().ToQuantity(1f);

                                //if (!SCPMain.skipWeightCalculationForContainer) continue;
                            }

                            if (SCPMain.decorationJustDuped)
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
                        if (SCPMain.decorationJustDuped)
                        { 
                            carryable.isInstance = true;
                        }
                        carryable.RetrieveAdditionalData();
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

                }

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
                //ManagePlacement.offset = Vector3.zero;
            }
            internal static void Postfix()
            {
                SCPMain.decorationJustDuped = false;

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
    }

}

namespace SCPlus
{

    internal class DecorationPatches
    {
        public static bool injectPdids = false;

        [HarmonyPatch(typeof(DecorationItem), nameof(DecorationItem.Start))]
        private static class DecorationInitialSetup
        {
            internal static void Postfix(ref DecorationItem __instance)
            {
                string name = SanitizeObjectName(__instance.name);

                if (CarryableData.blacklist.Contains(name))
                { 
                    __instance.m_AllowInInventory = false;
                    __instance.enabled = false;
                }

                
                if (CarryableData.blacklistSpecific.ContainsKey(GameManager.m_ActiveScene))
                {
                    foreach (var entry in CarryableData.blacklistSpecific[GameManager.m_ActiveScene]) 
                    {
                        if (entry.name == name)
                        {
                            if (WithinDistance(__instance.transform.position, entry.pos, 0.1f))
                            {
                                __instance.m_AllowInInventory = false;
                                __instance.enabled = false;
                            }
                        }
                    }
                }

                BreakDown bd = __instance.GetComponentInChildren<BreakDown>();

                if (!__instance.m_AllowInInventory)
                {
                    __instance.m_AllowInInventory = true;

                    SCPMain.RelevantSetupForDecorationItem(__instance, true);

                    if (bd)
                    {
                        bd.m_AllowEditModePlacement = true;
                    }
                }

                SCPMain.SetLayersToInteractiveProp(__instance);


                /*
                int s = 0;

                if (!__instance.name.Contains("OBJ_MetalBarrelA_Prefab")) return;
                //__instance.GatherRenderers();
                var rr = __instance.GetRenderers();
                foreach (Renderer r in rr)
                {
                    //MelonLogger.Msg(CC.Green, r.name);
                    if (r && r.isPartOfStaticBatch)
                    {
                        s++;
                    }
                    if (!r)
                    {
                        s++;
                    }
                    else if (r.IsNullOrDestroyed())
                    {
                        s++;
                    }
                    if(!__instance.HasRenderer(r))
                    {
                        s++;
                    }
                }

                MelonLogger.Msg($"{__instance.name} num bad renderers: {s}, array size: {rr.Count}");

                if (s == rr.Count)
                {
                    if (!bd || (bd && !bd.enabled))
                    {
                        __instance.enabled = false; // no customization for static batch objects
                    }
                }

                //Placeable pl = __instance.GetOrAddComponent<Placeable>();
                /*
                foreach (DecorationPlacePoint dpp in __instance.transform.GetComponentsInChildren<DecorationPlacePoint>())
                { 
                    dpp.gameObject.layer = vp_Layer.InteractivePropNoCollidePlayer;
                }
                */
                //pl.Awake();

            }
        }


        [HarmonyPatch(typeof(DecorationItem), nameof(DecorationItem.OnStartCustomization))]
        private static class StartCustomizationOnItem
        {
            internal static void Postfix(ref DecorationItem __instance)
            {
                //if (!__instance.isActiveAndEnabled) return;

                SCPMain.DisableNormalInteraction(__instance);

                int s = 0;
                foreach (Renderer r in __instance.GetRenderers())
                {
                    bool staticFlag = (r && (r.isPartOfStaticBatch || r.IsNullOrDestroyed())) || !r; 
                    if (staticFlag)
                    {
                        //MelonLogger.Msg(CC.Green, "Static batched renderer found, disabling outline");
                        s++;
                        __instance.TryGetComponent(out BreakDown bd);
                        if (!bd || !bd.isActiveAndEnabled)
                        {
                            if (r) r.SetPropertyBlock(null);
                        }
                    }
                }
                //MelonLogger.Msg(CC.Yellow, __instance.name + " array:" + __instance.GetRenderers().Count + " bad:" + s);


                if (s == __instance.GetRenderers().Count)
                {
                    //MelonLogger.Msg(CC.Yellow, "All renderers are static batched, disabling customization");
                    __instance.enabled = false;
                }

                if (__instance.TryGetComponent(out Placeable p))
                {
                    //MelonLogger.Msg(__instance.name);
                    if (string.IsNullOrEmpty(p.m_Guid))
                    {
                        //MelonLogger.Msg("1");
                        //MelonLogger.Msg("guid missing");
                        p.Awake();
                        //__instance.FinalizePlacementRecursive(true, true);
                        //p.m_Guid = Guid.NewGuid().ToString();

                    }
                }
            }
        }

        [HarmonyPatch(typeof(DecorationItem), nameof(DecorationItem.OnStopCustomization))]
        private static class StopCustomizationOnItem
        {
            internal static void Postfix(ref DecorationItem __instance)
            {

                SCPMain.RestoreNormalInteraction(__instance);
            }
        }

        [HarmonyPatch(typeof(DecorationItem), nameof(DecorationItem.OnPickup))]
        private static class PreventPickingUp
        {
            internal static bool Prefix(ref DecorationItem __instance)
            {
                
                WoodStove ws = __instance.GetComponentInChildren<WoodStove>();
                if (ws && ws.Fire.IsBurning())
                {
                    GameAudioManager.PlayGUIError();
                    HUDMessage.AddMessage(Localization.Get("SCP_Action_CantPickupHot"), false, true);
                    __instance.m_ActionPicker.TrySetEnabled(false);
                    return false;
                }

                CarryableData.SetupCarryable(__instance, true);

                if (__instance.IconReference == null || !__instance.IconReference.RuntimeKeyIsValid())
                {
                    SCPMain.RelevantSetupForDecorationItem(__instance);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(DecorationItem), nameof(DecorationItem.SetupInteraction))]
        private static class AddBreakDownToActions
        {

            internal static void Postfix(ref DecorationItem __instance)
            {
                BreakDown bd = __instance.GetComponentInChildren<BreakDown>();
                if (bd?.isActiveAndEnabled == true)
                {

                    Action a = () => bd.PerformInteraction();
                    __instance.m_ItemData.Add(new ActionPickerItemData("ico_harvest", "GAMEPLAY_BreakDown", a));
                }
            }
        }


        [HarmonyPatch(typeof(Inventory), nameof(Inventory.MaybeAdd))] // isn't applied to carryables
        private static class InitialSetupForDecorationsInInventory
        {
            internal static void Postfix(ref Inventory __instance, ref GameObject go)
            {
                //MelonLogger.Msg(CC.Red, __instance.m_DecorationItems.Count);
                go.TryGetComponent(out DecorationItem di);

                if (!di) return;

                if (di.IconReference == null || !di.IconReference.RuntimeKeyIsValid()) // icon not set
                {
                    SCPMain.RelevantSetupForDecorationItem(di);
                }
            }
        }


        [HarmonyPatch(typeof(Container), nameof(Container.Deserialize))]
        private static class InitialSetupForDecorationsInContainer
        {
            internal static void Postfix(ref Container __instance)
            {
                foreach (DecorationItem di in __instance.m_DecorationItems)
                {
                    if (di.IconReference == null || !di.IconReference.RuntimeKeyIsValid()) // icon not set
                    {
                        SCPMain.RelevantSetupForDecorationItem(di);
                    }
                }
            }
        }


        [HarmonyPatch(typeof(Placeable), nameof(Placeable.Awake))]
        private static class InjectGuidsBeforeTheyRealize
        {
            internal static void Prefix(ref Placeable __instance)
            {
                if (!injectPdids) return;

                if (__instance.GetComponent<ObjectGuid>())
                {
                    //Log(CC.Yellow, "Placeable: ObjectGuid already exists " + __instance.GetComponent<ObjectGuid>().m_Guid + " " + __instance.name);
                    return;
                }

                ObjectGuid og = __instance.GetOrAddComponent<ObjectGuid>();

                if (!string.IsNullOrEmpty(__instance.m_Guid))
                {
                    //Log(CC.Blue, "Placeable: Guid already generated " + __instance.m_Guid + " " + __instance.name);
                    return;
                }

                if (WithinDistance(__instance.transform.position, Vector3.zero))
                {
                    //Log(CC.Red, "Placeable: Can't generate guid - coords are 0 " + __instance.m_Guid + " " + __instance.name);
                    return;
                }

                //MelonLogger.Msg(CC.Cyan, "Placeable.Awake START " + __instance.transform.position.ToString("F4") + __instance.name);

                //__instance.transform.parent = PlaceableManager.FindOrCreateCategoryRoot().transform;
                //PlaceableManager.Add(__instance);
                Guid newGuid = GenerateSeededGuid(SeedFromCoords(__instance.transform.position));
                //og.m_RuntimeCachedPdid = GenerateSeededGuid(SeedFromCoords(__instance.transform.position)).ToString();
                //og.MaybeRuntimeRegister();

                Log(PdidTable.GetGameObject(og.m_Guid) ? CC.Green : CC.Gray, $"name {__instance.name} | PDID {og.m_RuntimeCachedPdid} | seed {SeedFromCoords(__instance.transform.position)} | coords {__instance.transform.position.ToString("F4")}");
                PdidTable.RuntimeAddOrReplace(og, newGuid.ToString());
                //PdidTable.s_RuntimeGameObjectByPdid[og.m_RuntimeCachedPdid] = og;

            }
        }
    }
}

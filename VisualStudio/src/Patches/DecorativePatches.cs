using Il2CppTLD.PDID;
using Il2CppVLB;

namespace SCPlus
{
    internal class DecorativePatches
    {

        public static bool injectPdids = false;

        [HarmonyPatch(typeof(DecorationItem), nameof(DecorationItem.Awake))]
        private static class MakeStuffPickupable
        {
            internal static void Postfix(ref DecorationItem __instance)
            {
               
                if (Settings.options.pickupAnything && !__instance.m_AllowInInventory)
                {
                    if (!__instance.IconReference.RuntimeKeyIsValid()) // icon not set
                    {
                        SCPMain.SetupDecorationItem(__instance); // maybe only for those that are in inventory? what about containers though
                    }
                    __instance.m_AllowInInventory = true;
                    if (__instance.GetComponentInChildren<BreakDown>())
                    {
                        __instance.GetComponentInChildren<BreakDown>().m_AllowEditModePlacement = true;
                    }
                }
                else if (Settings.options.pickupContainers && __instance.gameObject.layer == vp_Layer.Container && !__instance.m_AllowInInventory)
                {
                    __instance.m_AllowInInventory = true;
                }
                if (Settings.options.enableCustomizationAnywhere)
                {
                    int s = 0;
                    foreach (Renderer r in __instance.GetRenderers())
                    {
                        if (r && r.isPartOfStaticBatch)
                        {
                            s++;
                        }
                    }
                    if (s == __instance.GetRenderers().Count)
                    {
                        __instance.enabled = false;
                    }
                    if (__instance.enabled)
                    {
                        foreach (Collider c in __instance.GetComponentsInChildren<Collider>())
                        {
                            if (c.gameObject.layer == vp_Layer.Default || c.gameObject.layer == vp_Layer.TerrainObject) c.gameObject.layer = vp_Layer.InteractiveProp;
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(DecorationItem), nameof(DecorationItem.OnStartCustomization))]
        private static class RemoveOutlineFromBatchMesh
        {
            internal static void Postfix(ref DecorationItem __instance)
            {
                if (Settings.options.enableCustomizationAnywhere)
        
                {
                    foreach (Renderer r in __instance.GetRenderers())
                    {
                        if (r && r.isPartOfStaticBatch)
                        {
                            //__instance.enabled = false;
                            r.SetPropertyBlock(null);
                            //propertyBlock = null;
                            //MelonLogger.Msg(r.name);
                        }
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(DecorationItem), nameof(DecorationItem.OnPickup))]
        private static class PreventPickingUp
        {
            internal static bool Prefix(ref DecorationItem __instance)
            {
                if (Settings.options.pickupAnything)
                {
                    WoodStove ws = __instance.GetComponent<WoodStove>();
                    if (ws && ws.Fire.IsBurning())
                    {
                        GameAudioManager.PlayGUIError();
                        HUDMessage.AddMessage(Localization.Get("SCP_Action_CantPickupHot"), false, true);
                        __instance.m_ActionPicker.TrySetEnabled(false);
                        return false;
                    }
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(BreakDown), nameof(BreakDown.PerformInteraction))]
        private static class PreventDestroy
        {
            internal static bool Prefix(ref BreakDown __instance)
            {
                if (Settings.options.pickupAnything && __instance.m_EditModeDestroyOnly)
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






        /*
        [HarmonyPatch(typeof(DecorationItem), nameof(DecorationItem.Awake))]
        private static class get1
        {
            internal static void Prefix(ref DecorationItem __instance)
            {
                if (__instance?.name.Contains("PlasticBarrelA") != true) return;

                //MelonLogger.Msg(CC.Green, "DecorationItem.Awake START " + __instance.name);

            }
            internal static void Postfix(ref DecorationItem __instance)
            {
                if (__instance?.name.Contains("PlasticBarrelA") != true) return;
                //MelonLogger.Msg(CC.DarkGreen, "DecorationItem.Awake END " + __instance.transform.position.ToString("F4") + __instance.name);
            }
        }

        */



        [HarmonyPatch(typeof(Placeable), nameof(Placeable.Awake))]
        private static class InjectGuidsBeforeTheyRealise
        {
            internal static void Prefix(ref Placeable __instance)
            {
                if (!injectPdids) return;
                //if (__instance?.name.Contains("PlasticBarrelA") != true) return;
                if (__instance.GetComponent<ObjectGuid>())
                {
                    Log(CC.Yellow, "Placeable: ObjectGuid already exists " + __instance.GetComponent<ObjectGuid>().m_Guid + " " + __instance.name);
                    return;
                }
                ObjectGuid og = __instance.GetOrAddComponent<ObjectGuid>();
                if (!string.IsNullOrEmpty(__instance.m_Guid))
                {
                    Log(CC.Blue, "Placeable: Guid already generated " + __instance.m_Guid + " " + __instance.name);
                    return;
                }

                if (WithinDistance(__instance.transform.position, Vector3.zero))
                {
                    Log(CC.Red, "Placeable: Can't generate guid - coords are 0 " + __instance.m_Guid + " " + __instance.name);
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
            
            internal static void Postfix(ref Placeable __instance)
            {
                //if (__instance?.name.Contains("PlasticBarrelA") != true) return;
                //MelonLogger.Msg(CC.DarkCyan, "Placeable.Awake END " + __instance.transform.position.ToString("F4") + __instance.name);
                
                //MelonLogger.Msg(PdidTable.GetGameObject(__instance.m_Guid) ? CC.Green : CC.Red, "registered?"); //probably later? shows red but works
            }
            
        }
        /*
        [HarmonyPatch(typeof(Placeable), nameof(Placeable.FindOrCreateAndDeserialize))]
        private static class get3
        {
            internal static void Prefix(ref string guid, ref PlaceableSaveData data)
            {
                //MelonLogger.Msg("1");
                //if (__instance?.name?.Contains("PlasticBarrelA") != true) return;
                //MelonLogger.Msg("2");
                //MelonLogger.Msg(CC.Blue, "Placeable.Deserialize START " + __instance?.transform?.position.ToString("F4") + __instance?.name);
                //MelonLogger.Msg("3");
                //MelonLogger.Msg(data.m_Name.Contains("PlasticBarrelA") ? CC.Blue : CC.Gray, string.IsNullOrEmpty(guid) ? "no guid " : guid + data.m_Position);
                //MelonLogger.Msg(string.IsNullOrEmpty(data.ToString()) ? "no guid?" : data.m_Position);

            }
        }

        [HarmonyPatch(typeof(Placeable), nameof(Placeable.Serialize))]
        private static class get4
        {
            internal static void Postfix(ref Placeable __instance, ref PlaceableSaveData __result)
            {
                //MelonLogger.Msg("1");
                //if (__instance?.name?.Contains("PlasticBarrelA") != true) return;
                //MelonLogger.Msg("2");
                //MelonLogger.Msg(CC.Blue, "Placeable.Deserialize START " + __instance?.transform?.position.ToString("F4") + __instance?.name);
                //MelonLogger.Msg("3");
                MelonLogger.Msg(__result.m_Name.Contains("PlasticBarrelA") ? CC.Yellow : CC.Gray, __result.m_Position + " " + __result.m_Name);
                //MelonLogger.Msg(string.IsNullOrEmpty(data.ToString()) ? "no guid?" : data.m_Position);

            }
        }
        
        [HarmonyPatch(typeof(PlaceableManager), nameof(PlaceableManager.Deserialize))]
        private static class loada
        {
            internal static void Prefix()
            {
                SaveGameSlots.TryLoadDataFromSlot<SceneSaveGameFormat>("sandbox3", "WhalingShipA", out SceneSaveGameFormat data);
                MelonLogger.Msg(data.m_PlacementListSerialized.Count);
                foreach (PlaceableInfoSaveData pl in data.m_PlacementListSerialized)
                {
                    if (pl == null) continue;
                    MelonLogger.Msg(pl.m_Guid);

                    if (pl.m_Serialized == null) continue;
                    MelonLogger.Msg(pl.m_Serialized.m_Name.Contains("PlasticBarrelA") ? CC.Blue : CC.DarkGray, $"{pl.m_Serialized.m_Name}_ _{pl.m_Guid}_ _{pl.m_Serialized.m_Position}");

                }

            }
        }
        */
        
    }
}

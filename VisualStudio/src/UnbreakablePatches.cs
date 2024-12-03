using Il2CppTLD.Placement;
using UnityEngine;

namespace BCP
{
    internal class UnbreakablePatches
    {

        [HarmonyPatch(typeof(DecorationItem), nameof(DecorationItem.Awake))]
        private static class MakeContainersPickupable
        {
            internal static void Postfix(ref DecorationItem __instance)
            {
                if (Settings.options.pickupAnything && !__instance.m_AllowInInventory)
                {

                    __instance.m_AllowInInventory = true;
                    __instance.m_Weight = Il2CppTLD.IntBackedUnit.ItemWeight.FromKilograms(1f);
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
                        if (r.isPartOfStaticBatch)
                        {
                            s++;
                            //r.SetPropertyBlock(null);
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
        [HarmonyPatch(typeof(SafehouseManager), nameof(SafehouseManager.InCustomizableSafehouse))]
        private static class AlwaysCustomizable
        {
            internal static void Postfix(ref SafehouseManager __instance, ref bool __result)
            {
                if (Settings.options.enableCustomizationAnywhere) __result = true;
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
                        if (r.isPartOfStaticBatch)
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

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.StartPlaceMesh), [typeof(GameObject), typeof(float), typeof(PlaceMeshFlags), typeof(PlaceMeshRules)])]
        private static class ManagePlacement
        {
            internal static void Prefix(PlayerManager __instance, ref GameObject objectToPlace)
            {
                if (!Settings.options.pickupAnything) return;

                DecorationItem di = objectToPlace.GetComponent<DecorationItem>();

                if (di)
                {
                    foreach (Renderer r in di.GetRenderers())
                    {
                        r.enabled = true;
                    }
                    foreach (DecorationItem child in di.DecorationChildren)
                    { 
                        child.gameObject.active = true;
                    }
                    Container c = objectToPlace.GetComponent<Container>();
                    if (c)
                    {
                        c.m_Inspected = true;
                        c.m_StartInspected = true;
                    }
                }
            }
        }
    }
}
 
using Il2Cpp;

namespace SCPlus
{
    internal class DecorationHelper
    {
        public static Dictionary<string, float> autoWeightTable = new(); // object name | weight
        public static Dictionary<string, float> adjustedVanillaWeightTable = new(); // object name | weight

        public static bool instantiatingCarryables;
        public static bool decorationJustDuped;

        public static int carryableCoroutineCounter = 0;

        public static PlaceMeshRules genericPlacementRules = PlaceMeshRules.Default | PlaceMeshRules.AllowFloorPlacement | PlaceMeshRules.IgnoreCloseObjects;
        public static PlaceMeshRules boxPlacementRules = PlaceMeshRules.Default | PlaceMeshRules.AllowFloorPlacement | PlaceMeshRules.IgnoreCloseObjects | PlaceMeshRules.AllowStacking;
        public static PlaceMeshRules wallPlacementRules = PlaceMeshRules.Default | PlaceMeshRules.AllowWallPlacement | PlaceMeshRules.IgnoreCloseObjects;

        public enum DecoLayerMask
        {
            Default = 1,
            PossibleDecoration = 787009, // Default(0), Decoration(6), TerrainObject(9), Container(18), InteractiveProp(19)

        }

        public static void LookupPotentialCarryables(string sceneName)
        {
            Scene currentScene = SceneManager.GetSceneByName(sceneName);

            int n = 0;

            if (sceneName.EndsWith("_SANDBOX"))
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                foreach (var found in FindObjectsOfTypeInScene<WoodStove>(currentScene))
                {
                    if (TryGetCarryableRoot(found.transform, out GameObject? root))
                    {
                        MakeIntoDecoration(root);
                        n++;
                    }
                }
                foreach (var found in FindObjectsOfTypeInScene<MillingMachine>(currentScene))
                {
                    if (TryGetCarryableRoot(found.transform, out GameObject? root))
                    {
                        MakeIntoDecoration(root);
                        n++;
                    }
                }

                foreach (var found in FindObjectsOfTypeInScene<AmmoWorkBench>(currentScene))
                {
                    if (TryGetCarryableRoot(found.transform, out GameObject? root))
                    {
                        MakeIntoDecoration(root);
                        n++;
                    }
                }

                foreach (var found in FindObjectsOfTypeInScene<ObjectAnim>(currentScene)) // containers
                {
                    if (TryGetCarryableRoot(found.transform, out GameObject? root))
                    {
                        MakeIntoDecoration(root, boxPlacementRules);
                        n++;
                    }
                }
                foreach (var found in FindObjectsOfTypeInScene<InteractiveLightsource>(currentScene)) // mine lanterns
                {
                    if (TryGetCarryableRoot(found.transform, out GameObject? root))
                    {
                        MakeIntoDecoration(root, wallPlacementRules);
                        n++;
                    }
                }
                stopwatch.Stop();
                Log(CC.Blue, $"SC+ Lookup pass 2: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedTicks} ticks)");
            }
            else if (sceneName.EndsWith("_DLC01"))
            {
                foreach (var found in FindObjectsOfTypeInScene<TraderRadio>(currentScene))
                {
                    if (TryGetCarryableRoot(found.transform, out GameObject? root))
                    {
                        MakeIntoDecoration(root);
                        n++;
                    }
                }
            }
            else if (!sceneName.Contains("_"))
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                foreach (var found in FindObjectsOfTypeInScene<WoodStove>(currentScene))
                {
                    if (TryGetCarryableRoot(found.transform, out GameObject? root))
                    {
                        MakeIntoDecoration(root);
                        n++;
                    }
                }

                stopwatch.Stop();
                Log(CC.Blue, $"SC+ Lookup pass 1: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedTicks} ticks)");
            }

            if (n > 0) Log(CC.Gray, $"Turning [{n}] objects into carryables in scene {sceneName}");

            if (BreakDownPatches.HandleBreakdown.missing.Count > 0)
            {
                Log(CC.Yellow, $"Following objects are missing breakdown definitions: \n{string.Join(", \n", BreakDownPatches.HandleBreakdown.missing)}");
                BreakDownPatches.HandleBreakdown.missing.Clear();
            }

            if (BreakDownPatches.HandleBreakdown.toLoad.Count > 0)
            {
                foreach (var (bd, def) in BreakDownPatches.HandleBreakdown.toLoad)
                {
                    BreakDownHelper.LoadBreakDownFromDefinition(bd, def);
                }
                Log(CC.Gray, $"Loaded breakdown definitions for [{BreakDownPatches.HandleBreakdown.toLoad.Count}] objects");
                BreakDownPatches.HandleBreakdown.toLoad.Clear();
            }
        }

        public static void AdjustDecorationWeight(DecorationItem di)
        {
            if (!di) return;
            string name = SanitizeObjectName(di.name);
            if (adjustedVanillaWeightTable.ContainsKey(name)) return; // already adjusted
            di.m_Weight = ItemWeight.FromKilograms(di.m_Weight.ToQuantity(1f) * Settings.options.globalWeightModifier);
        }

        public static void RelevantSetupForDecorationItem(DecorationItem di, bool weightOnly = false) // icons and weight
        {
            if (!di) return;

            float weight = 1f;
            float baseWeight = 2f;
            float volume = 1f;
            string name = SanitizeObjectName(di.name);
            bool change = true;

            string logLine = "Calculated";

            if (CarryableData.decorationOverrideData.ContainsKey(name) && CarryableData.decorationOverrideData[name].weight != 0f)
            {
                weight = CarryableData.decorationOverrideData[name].weight;
                logLine = "Preset";
            }
            else if (di.name.ToLower().StartsWith("obj_curtain"))
            {
                weight = 0.5f;
            }
            else if (autoWeightTable.ContainsKey(name))
            {
                weight = autoWeightTable[name];
                logLine = "Precalculated";
            }
            else if (Settings.options.doWeightCalculation)
            {
                switch (di.tag)
                {
                    case "Wood":
                        weight = baseWeight * 0.66f;
                        break;
                    case "Metal":
                        weight = baseWeight * 2.0f;
                        if (di.name.ToLower().Contains("barrel")) weight *= 0.5f;
                        break;
                    case "Rug":
                        weight = baseWeight * 0.25f;
                        break;
                    case "Glass":
                        weight = baseWeight * 0.5f;
                        break;
                    default:
                        weight = baseWeight;
                        break;
                }
                if (weight == baseWeight)
                {
                    if (di.name.ToLower().Contains("wood")) weight *= 0.66f;
                    if (di.name.ToLower().Contains("metal")) weight *= 2.0f;
                    if (di.name.ToLower().Contains("sack")) weight *= 0.25f;
                    if (di.name.ToLower().Contains("computer")) weight *= 0.5f;
                    if (di.name.ToLower().Contains("lamp")) weight *= 0.25f;
                }
                foreach (var mf in di.GetComponentsInChildren<MeshFilter>())
                {
                    volume += mf.sharedMesh.bounds.GetVolumeCubic();
                }

                weight = Mathf.Round(volume * weight * 100f / 25f) * 25f / 100f; //round to 0.25
                weight = Mathf.Clamp(weight, 0.1f, 30f);
                weight *= Settings.options.autoWeightMultiplier;
                //weight *= Settings.options.globalWeightModifier;

                autoWeightTable.Add(name, weight);
            }
            else
            {
                change = false;
                logLine = "No change";
                weight = di.m_Weight.ToQuantity(1f);
                volume = -1f;
            }

            //if (logLine != "Precalculated") Log(CC.DarkGray, $"{logLine} for {name}: weight: {weight}, volume: {volume}");

            if (change) di.m_Weight = ItemWeight.FromKilograms(weight *= Settings.options.globalWeightModifier);

            if (weightOnly) return;

            if (SCPMain.catalogParsed.ContainsKey(name))
            {
                di.m_IconReference = new(SCPMain.catalogParsed[name]);
                if (!di.m_IconReference.RuntimeKeyIsValid())
                {
                    Log(CC.Red, $"Inconsistent icon GUID for decoration: {name}. Reverting to placeholder");
                    di.m_IconReference = new(SCPMain.catalogParsed[placeholderIconName]);
                }
            }
            else
            {
                di.m_IconReference = new(SCPMain.catalogParsed[placeholderIconName]);
                Log(CC.Magenta, $"Missing icon for decoration: {name}");
            }
        }

        public static void SetLayersToInteractiveProp(DecorationItem di)
        {
            foreach (string name in CarryableData.skipLayerChange)
            {
                if (di.name.ToLower().StartsWith(name.ToLower())) return;
            }

            foreach (Collider c in di.GetComponentsInChildren<Collider>())
            {
                if (c.gameObject.layer == vp_Layer.Default || c.gameObject.layer == vp_Layer.TerrainObject)
                {

                    c.gameObject.layer = vp_Layer.InteractiveProp;
                }
            }
            if (di.gameObject.layer == vp_Layer.Default || di.gameObject.layer == vp_Layer.TerrainObject)
            {
                di.gameObject.layer = vp_Layer.InteractiveProp;
            }
        }

        public static void DisableNormalInteraction(DecorationItem di)
        {
            //di.enabled = true;

            if (di.GetComponent<TraderRadio>())
            {
                di.GetComponent<TraderRadio>().enabled = false;
                di.GetComponent<BlockPlacement>().m_BlockDecorationItemPlacement = false;
            }

            if (di.GetComponent<BreakDown>())
            {
                di.GetComponent<BreakDown>().m_AllowEditModePlacement = ShouldAllowPlacement(di.gameObject);
                if (di.GetComponentInChildren<ObjectAnim>() ||
                    di.GetComponentInChildren<Container>() ||
                    di.GetComponentInChildren<Bed>()
                    ) // enable breakdown for custom breakables 
                    di.GetComponent<BreakDown>().enabled = true;
            }
            if (di.name.ToLower().Contains("forge"))
            {
                di.gameObject.layer = vp_Layer.InteractiveProp;
            }
            if (di.GetComponent<WoodStove>())
            {
                di.GetComponent<WoodStove>().enabled = false;
            }
        }

        public static void RestoreNormalInteraction(DecorationItem di)
        {
            //di.enabled = false;

            if (di.GetComponent<TraderRadio>())
            {
                di.GetComponent<TraderRadio>().enabled = true;
                //go.GetComponent<BlockPlacement>().m_BlockDecorationItemPlacement = true;
            }
            if (di.GetComponent<BreakDown>())
            {
                if (di.GetComponentInChildren<ObjectAnim>() ||
                    di.GetComponentInChildren<Container>() ||
                    di.GetComponentInChildren<Bed>()
                    ) // disable breakdown for custom breakables 
                    di.GetComponent<BreakDown>().enabled = false;
            }
            if (di.name.ToLower().Contains("forge"))
            {
                di.gameObject.layer = vp_Layer.TerrainObject;
            }
            if (di.GetComponent<WoodStove>())
            {
                di.GetComponent<WoodStove>().enabled = true;
            }
        }


        public static DecorationItem? MakeIntoDecoration(GameObject go, PlaceMeshRules rules = PlaceMeshRules.Default)
        {
            if (rules == PlaceMeshRules.Default) rules = genericPlacementRules;

            if (go && !go.GetComponentInChildren<DecorationItem>())
            {
                string name = SanitizeObjectName(go.name);

                LocalizedString ls = TryGetLocalizedName(go);
                DecorationItem di = go.AddComponent<DecorationItem>();
                SetLayersToInteractiveProp(di);

                di.m_DecorationPrefab = new(name);
                di.GetDecorationPrefab();
                di.m_DisplayName = ls;//bd ? bd.m_LocalizedDisplayName : new LocalizedString() { m_LocalizationID = "NaN" };
                di.GetCraftingDisplayName();
                /*
                if (name.ToLower().Contains("container"))
                {
                    di.m_PlacementRules = boxPlacementRules;
                }
                else
                {
                    di.m_PlacementRules = genericPlacementRules;
                }
                */
                //di.m_IconReference = new("");
                di.m_PlacementRules = rules;

                RelevantSetupForDecorationItem(di);
                //MelonLogger.Msg(CC.Blue, $"Decoration item {di.name} created with icon {di.m_IconReference.RuntimeKey.ToString()}");

                if (!GameManager.GetSafehouseManager().IsCustomizing()) RestoreNormalInteraction(di);

                return di;
            }

            return go.GetComponentInChildren<DecorationItem>(); // can be null
        }


        public static bool CurrentlyInNativeScene(SCPlusCarryable sc) => SceneManager.GetAllScenes().Contains(SceneManager.GetSceneByName(sc.nativeScene));
        public static bool CurrentlyInNativeScene(string native) => SceneManager.GetAllScenes().Contains(SceneManager.GetSceneByName(native));

        public static string TryGetGuidFromDecorationRoot(GameObject go)
        {
            //ObjectGuid og = GetRealParent(go.transform).GetComponentInChildren<ObjectGuid>();
            string guid = "";
            foreach (var og in GetRealParent(go.transform).GetComponentsInChildren<ObjectGuid>())
            {
                if (og.gameObject.GetComponent<Fire>()) continue;
                guid = og.GetPDID();
            }
            return guid;
        }

        public static bool TryGetCarryableRoot(Transform t, out GameObject? go)
        {
            while (t.parent
                && !CarryableData.carryablePrefabDefinition.ContainsKey(SanitizeObjectName(t.name)))
            {
                t = t.parent;
            }
            if (CarryableData.carryablePrefabDefinition.ContainsKey(SanitizeObjectName(t.name)))
            {
                go = t.gameObject;
                return true;
            }
            else
            {
                go = null;
                return false;
            }
        }

        public static bool FindCarryableAtPosition(string targetName, Vector3 position, float radius, out GameObject? go)
        {
            //MelonLogger.Msg(CC.Red, $"Looking for {targetName} at {position} with radius {radius}");
            Collider[] hits = Physics.OverlapSphere(position, radius, (int)DecoLayerMask.PossibleDecoration);

            foreach (var hit in hits)
            {
                //MelonLogger.Msg(CC.Red, "found" + hit.transform.name);
                if (TryGetCarryableRoot(hit.transform, out GameObject? parent))
                {
                    if (SanitizeObjectName(parent.name).ToLower().Contains(targetName.ToLower()))
                    {
                        //MelonLogger.Msg(CC.Red, parent.name + " contains " + targetName);
                        go = parent;
                        return true;
                    }
                }
            }
            go = null;
            return false;
        }

        public static LocalizedString TryGetLocalizedName(GameObject go)
        {
            string name = SanitizeObjectName(go.name);
            if (CarryableData.decorationOverrideData.ContainsKey(name))
            {
                string locID = CarryableData.decorationOverrideData[name].nameLocID;
                if (!string.IsNullOrEmpty(locID))
                {
                    return new LocalizedString() { m_LocalizationID = locID };
                }
            }

            BaseInteraction bi = go.GetComponentInChildren<BaseInteraction>();
            if (bi && !string.IsNullOrEmpty(bi.m_DefaultHoverText.m_LocalizationID))
            {
                return bi.m_DefaultHoverText;
            }
            BreakDown bd = go.GetComponentInChildren<BreakDown>();
            if (bd && !string.IsNullOrEmpty(bd.m_LocalizedDisplayName.m_LocalizationID))
            {
                return bd.m_LocalizedDisplayName;
            }

            if (Localization.Get(name) == name) // loc doesn't exist
            {
                //name = SanitizeObjectName(go.name);
                name = name.Split('_')[1];
                name = Regex.Replace(name, "(?<!^)([A-Z])", " $1"); // insert spaces before capital letters
                string[] s = name.Split(" ");
                if (s[s.Length - 1].Length < 2) name = name[..^2];
            }
            return new LocalizedString() { m_LocalizationID = name };
        }

        public static bool ShouldAllowPlacement(GameObject go)
        {
            if (go.layer == vp_Layer.Corpse) return Settings.options.allowMoveCorpses;
            else return true;
        }
    }
}

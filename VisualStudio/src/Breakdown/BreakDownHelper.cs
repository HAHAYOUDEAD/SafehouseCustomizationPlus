using Il2CppSystem.Runtime.Remoting;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler;
using static Il2Cpp.Utils;
using static UnityEngine.GridBrushBase;

namespace SCPlus
{
    internal class BreakDownHelper
    {
        private static Dictionary<string, BreakDownDefinition> allDefinitions = [];
        private static Dictionary<string, GameObject> cachedPrefabs = [];

        public static void PopulateDefinitions()
        {
            string[] defFiles = { "decoration", "exterior", "industrial", "interiors", "kitchen", "tech", "misc" };
            List<BreakDownDefinition> tempList = [];

            for (int i = 0; i < defFiles.Length; i++)
            {
                string data = ResourceHandler.LoadEmbeddedJSON($"{resourcesFolderForBreakDown}.{defFiles[i]}.json");

                try
                {
                    var def = JSON.Load(data).Make<List<BreakDownDefinition>>();
                    tempList.AddRange(def); ;
                }
                catch (FormatException e)
                {
                    Log(CC.Red, $"{Path.GetFileName(defFiles[i])} is incorrectly formatted");
                }
            }

            if (tempList.Count == 0)
            {
                Log(CC.Red, "No breakdown definitions found");
                return;
            }

            foreach (var def in tempList)
            {
                if (def.filter.Trim() == "" || def.filter == null)
                {
                    if (def.filters.Length > 0)
                    {
                        foreach (string filter in def.filters)
                        {
                            if (filter.Trim() != "" && filter != null)
                            {
                                allDefinitions[filter] = def;
                            }
                        }
                    }
                    continue;
                }
                allDefinitions[def.filter] = def;
            }
        }

        public static bool TryGetDefinitionForBreakDown(string decorationName, out BreakDownDefinition definition)
        {
            string sanitizedName = SanitizeObjectName(decorationName);
            definition = allDefinitions.FirstOrDefault(kvp => GetFilterResult(kvp.Key, sanitizedName)).Value;
            return definition != null;
        }

        private static bool GetFilterResult(string filter, string name)
        {
            if (filter.Contains("!"))
            {
                string[] parts = filter.Split('!');
                return name.Contains(parts[0]) && !name.Contains(parts[1]);
            }
            else
            {
                return name.Contains(filter);
            }
        }

        public static void LoadBreakDownFromDefinition(BreakDown bd, BreakDownDefinition def) // courtesy of Xpazeman's BetterBases
        {
            //Object yields
            if (def.yield != null && def.yield.Length > 0)
            {
                List<GameObject> itemYields = new List<GameObject>();
                List<int> numYield = new List<int>();

                foreach (BreakDownYield yield in def.yield)
                {
                    if (yield.item.Trim() != "")
                    {
                        GameObject? yieldItem;
                        string name = "GEAR_" + yield.item;

                        if (!cachedPrefabs.TryGetValue(name, out yieldItem) || yieldItem == null)
                        {
                            yieldItem = GearItem.LoadGearItemPrefab(name)?.gameObject;
                        }

                        if (yieldItem != null)
                        {
                            itemYields.Add(yieldItem);
                            numYield.Add(yield.num);
                        }
                        else
                        {
                            Log(CC.Yellow, "Yield GEAR_" + yield.item + " couldn't be loaded.");
                        }
                    }
                }

                bd.m_YieldObject = itemYields.ToArray();
                bd.m_YieldObjectUnits = numYield.ToArray();
            }
            else
            {
                bd.m_YieldObject = new GameObject[0];
                bd.m_YieldObjectUnits = new int[0];
            }

            //Time to harvest
            if (def.minutesToHarvest > 0)
                bd.m_TimeCostHours = def.minutesToHarvest / 60;
            else
                bd.m_TimeCostHours = 1f / 60;

            //Harvest sound
            /*MetalSaw				
              WoodSaw				
              Outerwear			
              MeatlSmall			
              Generic				
              Metal			
              MeatlMed				
              Cardboard			
              WoodCedar			
              NylonCloth			
              Plants				
              Paper				
              Wood					
              Wool					
              Leather				
              WoodReclaimedNoAxe	
              WoodReclaimed		
              Cloth				
              MeatLarge			
              WoodSmall			
              WoodFir				
              WoodAxe		*/

            if (def.sound.Trim() != "" && def.sound != null)
                bd.m_BreakDownAudio = "Play_Harvesting" + def.sound;
            else
                bd.m_BreakDownAudio = "Play_HarvestingGeneric";

            //Required Tools
            if (def.requireTool == true)
            {
                bd.m_RequiresTool = true;
            }

            if (def.tools != null && def.tools.Length > 0)
            {
                Il2CppSystem.Collections.Generic.List<GameObject> itemTools = new();

                foreach (String tool in def.tools)
                {
                    GameObject? selectedTool;
                    string name = "";

                    switch (tool.ToLower())
                    {
                        case "knife":
                            name = "GEAR_Knife";
                            break;
                        case "hacksaw":
                            name = "GEAR_Hacksaw";
                            break;
                        case "hatchet":
                            name = "GEAR_Hatchet";
                            break;
                        case "hammer":
                            name = "GEAR_Hammer";
                            break;
                    }

                    if (!cachedPrefabs.TryGetValue(name, out selectedTool) || selectedTool == null)
                    {
                        selectedTool = GearItem.LoadGearItemPrefab(name)?.gameObject;
                    }

                    if (selectedTool != null)
                    {
                        itemTools.Add(selectedTool);
                    }
                    else
                    {
                        Log(CC.Yellow, "Tool " + tool + " couldn't be loaded or doesn't exist");
                    }
                }

                Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<GameObject> toolsArray = new(itemTools.ToArray());

                if (toolsArray.Length > 0)
                {
                    bd.m_UsableTools = toolsArray;
                }
                else
                {
                    bd.m_RequiresTool = false;
                    bd.m_UsableTools = new GameObject[0];
                }
            }
            else
            {
                bd.m_UsableTools = new GameObject[0];
            }
        }
    }
}

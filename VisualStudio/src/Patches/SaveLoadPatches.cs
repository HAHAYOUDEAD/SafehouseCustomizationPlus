using UnityEngine.ResourceManagement.ResourceLocations;
using System.Diagnostics;
using Il2CppTLD.BigCarry;

namespace SCPlus
{

    internal class SaveLoadPatches // save-loading
    {




        [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.SaveSceneData))]
        private static class SaveOmittedData
        {
            internal static void Postfix(ref string sceneSaveName)
            {
                /*
                foreach (WoodStove ws in FireManager.m_WoodStoves)
                {
                    if (!ws) continue;
                    string guid = ws.GetComponent<ObjectGuid>()?.Get();
                    if (!string.IsNullOrEmpty(guid) && ws.Fire && ws.Fire.GetComponent<ObjectGuid>() == null)
                    {
                        SCPMain.fireBarrelData[guid] = ws.Fire.Serialize();
                    }
                }
                if (SCPMain.fireBarrelData.Count == 0) return;
                string serializedData = JSON.Dump(SCPMain.fireBarrelData);
                //dataManager.Save(serializedData, fireSaveDataTag);
                */
                //MelonLogger.Msg(CC.Red, "??????");
                string serializedMovablesForScene = CarryableManager.SerializeAll(false);
                //MelonLogger.Msg(CC.Red, serializedMovablesForScene);
                dataManager.Save(serializedMovablesForScene, movablesSaveDataTag + "_" + sceneSaveName);
                dataManager.Save(CarryableManager.SerializeAll(true), movablesSaveDataTag + "_carried");

            }
        }

        /*
        [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.RestoreGlobalData))] // before LoadSceneData so dict can poopulate before use
        private static class LoadOmittedData
        {
            internal static void Postfix()
            {
                string? serializedSaveData = dataManager.Load(fireSaveDataTag);
                

                if (!string.IsNullOrEmpty(serializedSaveData))
                {
                    JSON.MakeInto(JSON.Load(serializedSaveData), out SCPMain.fireBarrelData);
                }


            }
        }
        */

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.ResetLists))]
        private static class ClearCarryablesOnSceneChange
        {
            internal static void Postfix()
            {
                CarryableManager.Reset();
            }
        }


        [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.LoadSceneData))]
        private static class MakeStuffMovable
        {
            internal static void Postfix(ref string sceneSaveName)
            {



                string? serializedSaveData = dataManager.Load(movablesSaveDataTag + "_" + sceneSaveName);
                string? addSerializedSaveData = dataManager.Load(movablesSaveDataTag + "_carried");

                List<CarryableSaveDataProxy> dataList = new();

                if (!string.IsNullOrEmpty(serializedSaveData))
                {
                    JSON.MakeInto(JSON.Load(serializedSaveData), out dataList);
                }

                List<CarryableSaveDataProxy> addDataList = new();

                if (!string.IsNullOrEmpty(addSerializedSaveData))
                {
                    JSON.MakeInto(JSON.Load(addSerializedSaveData), out addDataList);
                }


                //Log(CC.Yellow, $"SC+ Deserializing time: {stopwatch.ElapsedMilliseconds} ms");
                //lastOperationTookTime = (int)stopwatch.ElapsedMilliseconds;


                //List<GameObject> fromGuids = new List<GameObject>();

                for (int i = dataList.Count - 1; i >= 0; i--) // existing decorations that only lack additional data
                {
                    var proxy = dataList[i];
                    if ((proxy.state & CS.ExistingDecoration) == CS.ExistingDecoration)
                    {
                        if (!string.IsNullOrEmpty(proxy.guid))
                        {
                            GameObject go = PdidTable.GetGameObject(proxy.guid);
                            if (go && !string.IsNullOrEmpty(proxy.dataToSave))
                            {
                                SCPlusCarryable c = go.AddComponent<SCPlusCarryable>();
                                CarryableManager.Add(c);
                                c.FromProxy(proxy, false);
                                c.RetrieveAdditionalData(proxy.dataToSave);
                                dataList.RemoveAt(i);
                            }
                        }
                    }
                }

                dataList = dataList.Concat(addDataList).ToList();

                //Log(CC.Yellow, $"SC+ Data parse time: {stopwatch.ElapsedMilliseconds - lastOperationTookTime} ms");
                //lastOperationTookTime = (int)stopwatch.ElapsedMilliseconds;

                // making objects movable and loading positions if was moved inside same scene






                //MelonCoroutines.Start(SCPMain.ProcessCarryables(dataList, Settings.options.carryableProcessingInterval));


                Stopwatch stopwatch = Stopwatch.StartNew();
                Stopwatch stopwatch2 = Stopwatch.StartNew();
                int lastOperationTookTime = 0;

                Log(CC.Red, $"SC+ Loading started");

                foreach (GameObject rootGo in GetRootParents())
                {
                    HashSet<GameObject> result = new();

                    foreach (var entry in CarryableData.carryablePrefabDefinition)
                    {
                        //MelonCoroutines.Start(GetChildrenWithNameEnum(rootGo, entry.Key, result));
                        GetChildrenWithName(rootGo, entry.Key, result);
                        /*
                        if (carryableCoroutineCounter > cutoff)
                        {
                            carryableCoroutineCounter = 0;
                            //Log("Frame skip");
                            yield return new WaitForEndOfFrame();
                        }
                        */
                    }


                    Log(CC.Yellow, $"SC+ Children of {rootGo.name} lookup time: {stopwatch.ElapsedMilliseconds - lastOperationTookTime} ms");
                    lastOperationTookTime = (int)stopwatch.ElapsedMilliseconds;

                    foreach (GameObject child in result)
                    {


                        if (child.IsNullOrDestroyed() || !child.active) continue;

                        DecorationItem di = SCPMain.MakeIntoDecoration(child);

                        if (dataList.Count == 0) continue;

                        for (int i = dataList.Count - 1; i >= 0; i--)
                        {
                            var data = dataList[i];

                            CarryableData.carryablePrefabDefinition.TryGetValue(data.name, out ObjectToModify? otm);

                            if (child.name.Contains(data.name))
                            {
                                // object still in native scene
                                if (data.IsInNativeScene() && ((data.state & CS.Removed) == 0)) //  && !data.TryGetContaier() 
                                {
                                    if (WithinDistance(data.originalPos, child.transform.position))
                                    {
                                        if (otm != null && otm.alwaysReplaceAfterFirstInteraction)
                                        {
                                            Log(CC.DarkCyan, $"In native scene, force removed {data.name} native: {data.nativeScene} current: {data.currentScene}");
                                            child.active = false;
                                            continue;
                                        }
                                        // in native scene and in container
                                        if (di && ((data.state & CS.InContainer) == CS.InContainer))
                                        {
                                            Log(CC.DarkCyan, $"In native scene, in container {data.name} native: {data.nativeScene} current: {data.currentScene}");
                                            data.TryGetContainer().AddDecorationItem(di);
                                        }
                                        else
                                        {
                                            Log(CC.DarkCyan, $"In native scene, moved {data.name} native: {data.nativeScene} current: {data.currentScene}");
                                        }

                                        SCPlusCarryable c = child.AddComponent<SCPlusCarryable>();
                                        CarryableManager.Add(c);
                                        c.FromProxy(data, true, true);
                                        dataList.RemoveAt(i);
                                    }
                                }
                                // object no longer in native scene, but player is
                                else if (CurrentlyInNativeScene(data.nativeScene))
                                {
                                    if (WithinDistance(data.originalPos, child.transform.position))
                                    {
                                        if ((data.state & CS.OnPlayer) == 0) // native object moved out of scene
                                        {
                                            if (!child.GetComponent<SCPlusCarryable>())
                                            {
                                                SCPlusCarryable c = child.AddComponent<SCPlusCarryable>();
                                                c.FromProxy(data, true, false, true);
                                                CarryableManager.Add(c);
                                                Log(CC.DarkYellow, $"Not in native scene or disabled, removed and enlisted {data.name} native: {data.nativeScene} current: {data.currentScene}");
                                            }
                                            else
                                            {
                                                Log(CC.DarkMagenta, $"Not in native scene or disabled, removed {data.name} native: {data.nativeScene} current: {data.currentScene}");
                                            }

                                            child.active = false;
                                            dataList.RemoveAt(i);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    Log(CC.Green, $"SC+ Children of {rootGo.name} operations time: {stopwatch.ElapsedMilliseconds - lastOperationTookTime} ms");
                    lastOperationTookTime = (int)stopwatch.ElapsedMilliseconds;
                }

                stopwatch.Stop();
                Log(CC.Red, $"SC+ Loading pass 1: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedTicks} ticks)");

                if (dataList.Count == 0) return;

                stopwatch.Restart();

                //SCPMain.instantiatingCarryables = true;
                GameObject globalParent = new GameObject("CarryableTemp");
                globalParent.SetActive(false);
                // instantiating remaining objects: in inventory, containers or different scene
                foreach (var data in dataList)
                {
                    //MelonLogger.Msg(CC.Red, $"{data.name}");
                    CarryableData.carryablePrefabDefinition.TryGetValue(data.name, out ObjectToModify? otm);
                    if (otm == null) continue;

                    GameObject instance = AssetHelper.SafeInstantiateAssetAsync(otm.existingDecoration ? data.name : otm.assetPath, globalParent.transform).WaitForCompletion();

                    if (otm.needsReconstruction)
                    {
                        instance = otm.reconstructAction.Invoke();
                    }

                    // prepare new instance
                    switch (data.type)
                    {
                        case CT.FlareGunCase:
                            foreach (PrefabSpawn ps in instance.GetComponentsInChildren<PrefabSpawn>())
                            {
                                ps.m_SpawnComplete = true;
                            }
                            break;
                        case CT.MillingMachine:
                            MelonCoroutines.Start(PrepareMillingMachine(instance));

                            break;
                        default:
                            break;
                    }

                    if (instance != null)
                    {
                        instance.name = data.name;
                        DecorationItem di = SCPMain.MakeIntoDecoration(instance);
                        SCPlusCarryable carryable = instance.AddComponent<SCPlusCarryable>();
                        carryable.isInstance = true;
                        bool shouldLoadAdditionalData = false;

                        if ((data.state & CS.OnPlayer) == CS.OnPlayer) // in inventory
                        {
                            //dupes when changing scene
                            Log(CC.Gray, $"Instantiating object in inventory | {data.name} native: {data.nativeScene} current: {data.currentScene}");
                            GameManager.GetInventoryComponent().AddDecoration(di);
                            instance.SetActive(false);
                        }
                        else if (data.TryGetContainer()) // in container
                        {
                            Log(CC.Gray, $"Instantiating object in container | {data.name} native: {data.nativeScene} current: {data.currentScene}");
                            data.TryGetContainer().AddDecorationItem(di);
                            instance.SetActive(false);
                        }
                        else //if (!data.IsInNativeScene() && GameManager.CompareSceneNames(GameManager.m_ActiveScene, data.currentScene)) // in different scene, or same scene but spawned additionally with console
                        {
                            Log(CC.Gray, $"Instantiating object in world | {data.name} native: {data.nativeScene} current: {data.currentScene}");
                            GameObject root = PlaceableManager.FindOrCreateCategoryRoot();
                            instance.transform.SetParent(root.transform);

                            shouldLoadAdditionalData = true;
                        }

                        CarryableManager.Add(carryable);
                        carryable.FromProxy(data, true);
                        if (shouldLoadAdditionalData) carryable.RetrieveAdditionalData(); // data.dataToSave
                    }
                    else
                    {
                        Log(CC.Red, $"Failed to instantiate {data.name}, check path: {otm.assetPath}");
                    }
                }
                GameObject.Destroy(globalParent);


                stopwatch.Stop();
                stopwatch2.Stop();
                Log(CC.Red, $"SC+ Loading pass 2: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedTicks} ticks)");
                Log(CC.Red, $"SC+ Total loading: {stopwatch2.ElapsedMilliseconds} ms ({stopwatch2.ElapsedTicks} ticks)");

            }


        }



        /*
        [HarmonyPatch(typeof(AssetHelper), nameof(AssetHelper.InstantiateAssetAsync))]
        private static class testtest
        {
            internal static void Prefix(ref IResourceLocation resourceLocation)
            { 
                string line = "failed";
                if (resourceLocation != null)
                {
                    line = $"Key: {resourceLocation.PrimaryKey} | InternalID: {resourceLocation.InternalId}";
                }
                MelonLogger.Msg(resourceLocation != null ? CC.Blue : CC.Red, line);
            }
        }
        */
    }
   
}

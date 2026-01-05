using UnityEngine.ResourceManagement.ResourceLocations;
using System.Diagnostics;
using Il2CppTLD.BigCarry;
using Il2CppNodeCanvas.Tasks.Actions;
using static SCPlus.CarryableData;

namespace SCPlus
{

    internal class SaveLoadPatches
    {

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.ResetLists))]
        private static class ClearCarryablesOnSceneChange
        {
            internal static void Postfix()
            {
                CarryableManager.Reset();
            }
        }

        [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.SaveSceneData))]
        private static class SaveOmittedData
        {
            internal static void Postfix(ref string sceneSaveName)
            {
                //string serializedMovablesForScene = CarryableManager.SerializeAll(false);
                //MelonLogger.Msg(CC.Red, serializedMovablesForScene);
                dataManager.Save(CarryableManager.SerializeAll(false), movablesSaveDataTag + "_" + sceneSaveName);
                dataManager.Save(CarryableManager.SerializeAll(true), movablesSaveDataTag + "_carried");

            }
        }

        [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.LoadSceneData))]
        private static class MakeStuffMovable
        {
            internal static void Postfix(ref string sceneSaveName)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

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

                for (int i = dataList.Count - 1; i >= 0; i--) 
                {
                    var proxy = dataList[i];
                    if ((proxy.state & CS.ExistingDecoration) != 0) // existing decorations that only lack additional data
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

                    // FEATURE remove placing restrictions, fix milling and ammobench height

                    if (proxy.nativeScene.Contains(sceneSaveName))
                    {
                        if (FindCarryableAtPosition(proxy.name, proxy.originalPos, 1f, out GameObject? go)) // if was manipulated, find in scene and disable
                        {
                            SCPlusCarryable c = go.AddComponent<SCPlusCarryable>();
                            CarryableManager.Add(c);
                            c.FromProxy(proxy, true);



                            if (carryablePrefabDefinition.TryGetValue(proxy.name, out ObjectToModify? otm) && !otm.pickupable) // if can't be picked up, just move to saved position
                            {

                                go.transform.SetPositionAndRotation(proxy.currentPos, proxy.currentRot);

                                //if (!string.IsNullOrEmpty(proxy.dataToSave)) c.RetrieveAdditionalData(proxy.dataToSave);
                                dataList.RemoveAt(i);

                                Log(CC.Gray, $"Moving {proxy.name} in scene | native: {proxy.nativeScene} current: {proxy.currentScene}");

                                continue;
                            }

                            go.active = false;
                            Log(CC.Gray, $"Disabling {proxy.name} in scene | native: {proxy.nativeScene} current: {proxy.currentScene}");


                            if ((proxy.state & CS.Removed) != 0)
                            {

                                dataList.RemoveAt(i);


                                continue;
                            }
                        }
                    }
                }

                dataList = dataList.Concat(addDataList).ToList();

                stopwatch.Stop();
                Log(CC.Blue, $"SC+ Prep pass: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedTicks} ticks)");

                if (dataList.Count == 0) return;

                stopwatch.Restart();

                SCPMain.instantiatingCarryables = true;
                GameObject globalParent = new GameObject("CarryableTemp");
                globalParent.SetActive(false);

                foreach (var data in dataList)
                {
                    carryablePrefabDefinition.TryGetValue(data.name, out ObjectToModify? otm);
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
                        case CT.AmmoWorkbench:
                        case CT.Container:
                            foreach (Container c in instance.GetComponentsInChildren<Container>())
                            {
                                c.m_DisableSerialization = true;
                            }
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

                        if ((data.state & CS.OnPlayer) != 0) // in inventory
                        {
                            //dupes when changing scene
                            Log(CC.Gray, $"Instantiating {data.name} in inventory | native: {data.nativeScene} current: {data.currentScene}");
                            GameManager.GetInventoryComponent().AddDecoration(di);
                            instance.SetActive(false);
                        }
                        else if (data.TryGetContainer()) // in container
                        {
                            Log(CC.Gray, $"Instantiating {data.name} in container | native: {data.nativeScene} current: {data.currentScene}");
                            data.TryGetContainer().AddDecorationItem(di);
                            instance.SetActive(false);
                        }
                        else // in scene
                        {
                            Log(CC.Gray, $"Instantiating {data.name} in scene | native: {data.nativeScene} current: {data.currentScene}");
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
                SCPMain.instantiatingCarryables = false;

                stopwatch.Stop();
                Log(CC.Blue, $"SC+ Init pass: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedTicks} ticks)");
            }
        }
    }
}

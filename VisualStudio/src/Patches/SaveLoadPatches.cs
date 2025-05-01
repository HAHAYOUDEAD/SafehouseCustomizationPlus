using UnityEngine.ResourceManagement.ResourceLocations;
using System.Diagnostics;
using Il2CppTLD.BigCarry;
using static Il2Cppgw.gql.Interpreter;
using Il2CppNodeCanvas.Tasks.Actions;

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

        [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
        private static class MakeContainersMovable
        {
            internal static void Postfix(ref Container __instance)
            {
                string name = SanitizeObjectName(__instance.name);
                if (CarryableData.carryablePrefabDefinition.ContainsKey(name))
                {
                    GameObject parent = GetRealParent(__instance.transform);
                    MelonLogger.Msg(parent.name);

                    SCPMain.MakeIntoDecoration(parent);
                }
            }
        }

        [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.LoadSceneData))]
        private static class MakeStuffMovable
        {
            internal static void Postfix(ref string sceneSaveName)
            {


                Stopwatch stopwatch = Stopwatch.StartNew();
                Log(CC.Red, $"SC+ Loading started");


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


                // if was manipulated, find in scene and disable
                foreach (var data in dataList)
                {
                    if (data.nativeScene == sceneSaveName)
                    {
                        if (FindRoughlyAtPosition(data.name, data.originalPos, 1f, out GameObject? go)) 
                        { 
                            go.active = false;
                        }
                    }
                }

                Scene currentScene = SceneManager.GetActiveScene();

                // find in scene and make into decoration
                foreach (var found in FindObjectsOfTypeInScene<WoodStove>(currentScene))
                {
                    GameObject parent = GetRealParent(found.transform);
                    //MelonLogger.Msg(parent.name);
                    SCPMain.MakeIntoDecoration(parent);
                }

                foreach (var found in FindObjectsOfTypeInScene<MillingMachine>(currentScene))
                {
                    GameObject parent = GetRealParent(found.transform);
                    //MelonLogger.Msg(parent.name);
                    SCPMain.MakeIntoDecoration(parent);
                }

                foreach (var found in FindObjectsOfTypeInScene<AmmoWorkBench>(currentScene))
                {
                    GameObject parent = GetRealParent(found.transform);
                    //MelonLogger.Msg(parent.name);
                    SCPMain.MakeIntoDecoration(parent);
                }

                if (SCPMain.hasTFTFTF)
                {
                    //MelonLogger.Msg(CC.Blue, "TFTFTF detected");
                    currentScene = SceneManager.GetSceneByName(currentScene.name + "_DLC01");
                    if (currentScene.IsValid())
                    {
                        //MelonLogger.Msg(CC.Blue, currentScene.name);
                        foreach (var found in FindObjectsOfTypeInScene<TraderRadio>(currentScene))
                        {
                            GameObject parent = GetRealParent(found.transform);
                            //MelonLogger.Msg(parent.name);
                            SCPMain.MakeIntoDecoration(parent);
                        }
                    }
                }

                stopwatch.Stop();
                Log(CC.Red, $"SC+ Loading pass 1: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedTicks} ticks)");

                if (dataList.Count == 0) return;

                stopwatch.Restart();

                SCPMain.instantiatingCarryables = true;
                GameObject globalParent = new GameObject("CarryableTemp");
                globalParent.SetActive(false);

                foreach (var data in dataList)
                {
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
                        else //if (!data.IsInNativeScene() && GameManager.CompareSceneNames(GameManager.m_ActiveScene, data.currentScene)) // in scene
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
                SCPMain.instantiatingCarryables = false;

                stopwatch.Stop();
                Log(CC.Red, $"SC+ Loading pass 2: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedTicks} ticks)");
            }


        }
    }
}

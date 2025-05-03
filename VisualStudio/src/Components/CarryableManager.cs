namespace SCPlus
{
    internal class CarryableManager
    {
        public static List<SCPlusCarryable> carryables = new();

        public static void Reset() => carryables.Clear();

        public static void Add(SCPlusCarryable s)
        {
            if (!carryables.Contains(s)) carryables.Add(s);
        }

        public static void Remove(SCPlusCarryable s)
        {
            if (carryables.Contains(s)) carryables.Remove(s);
        }

        public static string SerializeAll(bool carried)
        {
            List<CarryableSaveDataProxy> allDataInScene = new();
            List<CarryableSaveDataProxy> allDataCarried = new();

            var currentScenes = SceneManager.GetAllScenes();
            foreach (var c in carryables)
            {
                if (c?.gameObject)
                {
                    CarryableSaveDataProxy proxy;
                    if (CarryableData.carryablePrefabDefinition.ContainsKey(c.objectName) && CarryableData.carryablePrefabDefinition[c.objectName].existingDecoration)
                    {
                        proxy = c.ToProxy(true);
                    }
                    else
                    {
                        proxy = c.ToProxy(false);
                    }

                    bool onPlayer = (proxy.state & CS.OnPlayer) == CS.OnPlayer;
                    if (!carried &&
                        (!onPlayer || // not on player
                        (onPlayer && currentScenes.Contains(SceneManager.GetSceneByName(proxy.nativeScene)) && !c.isInstance))) // or in native scene and just picked up
                    {
                        if (onPlayer) // saving copy in original scene so object can be removed later
                        {
                            proxy.state &= ~CS.OnPlayer;
                            proxy.state |= CS.Removed;
                        }
                        if ((proxy.state & CS.Removed) == CS.Removed)
                        {
                            proxy.dataToSave = "";
                        }
                        bool alreadyExists = false;
                        for (int i = 0; i < allDataInScene.Count; i++)
                        {
                            if (proxy.name == allDataInScene[i].name && WithinDistance(proxy.originalPos, allDataInScene[i].originalPos))
                            {
                                if ((allDataInScene[i].state & CS.Removed) == CS.Removed)
                                {
                                    allDataInScene[i] = proxy;
                                }
                                alreadyExists = true;
                                break;
                            }
                        }
                        if (!alreadyExists) allDataInScene.Add(proxy);
                    }
                    if (carried && onPlayer)
                    {
                        proxy.state &= ~CS.Removed;
                        allDataCarried.Add(proxy);
                    }
                }
            }

            StringBuilder jsonArray = new StringBuilder();
            List<CarryableSaveDataProxy> relevantData = carried ? allDataCarried : allDataInScene;

            jsonArray.Append('[');
            if (relevantData.Count > 0)
            {
                foreach (var s in relevantData.Where(i => i != relevantData.Last()))
                {
                    jsonArray.Append(Extensions.JsonDumpSkipDefaults(s) + ',');
                }
                jsonArray.Append(Extensions.JsonDumpSkipDefaults(relevantData.Last()));
            }
            jsonArray.Append(']');

            return jsonArray.ToString();
        }
    }
}

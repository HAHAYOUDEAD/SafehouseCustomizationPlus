namespace SCPlus
{
    internal class TrustyPatches // save-loading
    {

        [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.SaveSceneData))]
        private static class SaveOmittedData
        {
            internal static void Postfix()
            {
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
                dataManager.Save(serializedData, fireSaveDataTag);
            }
        }


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

        [HarmonyPatch(typeof(Placeable), nameof(Placeable.Deserialize))]
        private static class LoadFireBarrelData
        {
            internal static void Postfix(ref Placeable __instance, ref string guid)
            {
                if (string.IsNullOrEmpty(guid)) return;

                WoodStove ws = __instance.GetComponent<WoodStove>();
                if (!ws) return; 

                if (SCPMain.fireBarrelData.ContainsKey(guid))
                {
                    ws.Fire.Deserialize(SCPMain.fireBarrelData[guid]);
                }
            }
        }
    }
}

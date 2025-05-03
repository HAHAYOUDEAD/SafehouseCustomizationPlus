namespace SCPlus
{
    [RegisterTypeInIl2Cpp]
    internal class SCPlusLargeObject : MonoBehaviour
    {
        public SCPlusLargeObject(IntPtr intPtr) : base(intPtr) { }

        public bool isDismantled;
        public List<GameObject> allowedTools;
    }

    [RegisterTypeInIl2Cpp]
    internal class SCPlusCarryable : MonoBehaviour
    {
        public SCPlusCarryable(IntPtr intPtr) : base(intPtr) { }

        public string objectName = "";
        public CT type;
        public string nativeScene = "";
        public Vector3 originalPos = Vector3.zero;
        public string additionalData = "";
        public bool isInstance = false;

        public void OnDestroy()
        {
            CarryableManager.Remove(this);
        }

        public CarryableSaveDataProxy ToProxy(bool shorten = false)
        {
            CS state = GetState();
            string data = "";
            bool nativeOrInactiveContainer = this.type == CT.Container && (!this.gameObject.activeInHierarchy || CurrentlyInNativeScene(this.nativeScene));
            if ((state & CS.Removed) == 0 && !nativeOrInactiveContainer)
            {
                if (string.IsNullOrEmpty(additionalData))
                {
                    data = GetAdditionalDataToSave();
                }
                else data = additionalData;
            }

            CarryableSaveDataProxy proxy;
            if (shorten)
            {
                proxy = new()
                {
                    name = this.objectName,
                    type = this.type,
                    dataToSave = data,
                    guid = TryGetGuid(this.gameObject),
                    state = CS.ExistingDecoration,
                };
            }
            else
            {
                string containerGuid = "";
                if (!this.gameObject.active && this.transform.parent.TryGetComponentInParent(out Container c)) 
                {
                    containerGuid = c.GetGuid();
                }
                proxy = new()
                {
                    name = this.objectName,
                    type = this.type,
                    nativeScene = this.nativeScene,
                    currentScene = this.gameObject.scene.name == "DontDestroyOnLoad" ? "" : this.gameObject.scene.name,
                    originalPos = this.originalPos,
                    currentPos = this.transform.position,
                    currentRot = this.transform.rotation,
                    dataToSave = data,
                    containerGuid = containerGuid,
                    state = state,
                };
            }
            return proxy;
        }

        public void FromProxy(CarryableSaveDataProxy proxy, bool full, bool ignoreAdditionalData = false, bool ignorePosition = false)
        {
            this.objectName = proxy.name;
            this.type = proxy.type;
            this.additionalData = ignoreAdditionalData ? "" : proxy.dataToSave;

            if (full)
            {
                this.nativeScene = proxy.nativeScene;
                this.originalPos = proxy.originalPos;
                if (!ignorePosition) this.transform.position = proxy.currentPos;
                if (!ignorePosition) this.transform.rotation = proxy.currentRot;
            }
        }

        public string GetAdditionalDataToSave()
        {
            string data = "";

            switch (type)
            {
                case CT.FlareGunCase:
                    data = "0000000000"; // 0 for gun, 1-8 for ammo, 9 for open state
                    foreach (GearPlacePoint gpp in this.GetComponentsInChildren<GearPlacePoint>(true))
                    {
                        int.TryParse(Regex.Replace(gpp.name, "[^0-9]", ""), out int index);
                        if (gpp.m_PlacedGear || gpp.FindGearAtPlacePoint()) data = data.Remove(index, 1).Insert(index, "1");
                    }
                    if (this.gameObject.active && this.GetComponentInChildren<OpenClose>().IsOpen())
                    {
                        data = data.Remove(9, 1).Insert(9, "1");
                    }
                    break;
                case CT.Stove:
                    WoodStove wsStove = this.GetComponentInChildren<WoodStove>();
                    if (wsStove && wsStove.Fire)
                    {

                        data = CompressDeflate(wsStove.Fire.Serialize());
                    }
                    break;
                case CT.AmmoWorkbench:
                    WoodStove wsAW = this.GetComponentInChildren<WoodStove>();
                    if (wsAW && wsAW.Fire)
                    {
                        data = CompressDeflate(wsAW.Fire.Serialize());
                    }
                    foreach (Container c in this.GetComponentsInChildren<Container>())
                    {
                        data += dataSeparator + CompressDeflate(c.Serialize());
                    }
                    break;
                case CT.Container:
                    bool first = true;
                    foreach (Container c in this.GetComponentsInChildren<Container>())
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            data += dataSeparator;
                        }
                        data += CompressDeflate(c.Serialize());
                    }
                    break;
                default:
                    break;
            }

            return data;
        }

        public void RetrieveAdditionalData(string data = "")
        {

            if (string.IsNullOrEmpty(data))
            {
                if (string.IsNullOrEmpty(additionalData))
                {
                    return;
                }
                else
                {
                    data = additionalData;
                }
            }

            string[] splitData;

            switch (type)
            {
                case CT.FlareGunCase:
                    int i = 0;
                    string item = "";

                    foreach (GearPlacePoint gpp in this.GetComponentsInChildren<GearPlacePoint>(true))
                    {

                        gpp.m_AddToHierarchy = true;
                        gpp.gameObject.GetOrAddComponent<ObjectGuid>().MaybeRuntimeRegister();

                        if (data[i] == '1' && !gpp.m_PlacedGear)
                        {
                            if (gpp.name.Contains(i.ToString())) // ammo
                            {
                                item = "GEAR_FlareGunAmmoSingle";
                            }
                            else // gun
                            {
                                item = "GEAR_FlareGun";
                            }

                            gpp.PlaceGear(gpp.FindGearAtPlacePoint() ?? GearItem.InstantiateGearItem(item), true);
                        }
                        i++;
                    }
                    if (data[9] == '1')
                    {
                        this.GetComponentInChildren<OpenClose>().Start();
                        this.GetComponentInChildren<OpenClose>().m_ForceOpenOnUpdate = true;
                    }
                    break;
                case CT.Stove:
                    WoodStove wsStove = this.GetComponentInChildren<WoodStove>();
                    if (wsStove && wsStove.Fire)
                    {
                        wsStove.Fire.Deserialize(DecompressDeflate(data));
                    }
                    break;
                case CT.AmmoWorkbench:
                    splitData = data.Split(dataSeparator);
                    WoodStove wsAW = this.GetComponentInChildren<WoodStove>();
                    if (wsAW && wsAW.Fire)
                    {
                        wsAW.Fire.Deserialize(DecompressDeflate(splitData[0]));
                    }
                    Container[] containersAW = this.GetComponentsInChildren<Container>();
                    for (int ii = 0; ii < containersAW.Count(); ii++)
                    {
                        if (splitData.Length > ii) containersAW[ii].Deserialize(DecompressDeflate(splitData[ii + 1]), null);
                    }
                    break;
                case CT.Container:
                    splitData = data.Split(dataSeparator);
                    Container[] containers = this.GetComponentsInChildren<Container>();
                    for (int ii = 0; ii < containers.Count(); ii++)
                    {
                        if (splitData.Length > ii) containers[ii].Deserialize(DecompressDeflate(splitData[ii]), null);
                    }
                    break;
                default:
                    break;
            }

            additionalData = string.Empty;
        }

        public CS GetState()
        {
            CS state = CS.None;

            if (!this.gameObject.activeInHierarchy)
            {
                if (this.gameObject.scene.name == "DontDestroyOnLoad") state |= CS.OnPlayer;

                else if (this.transform.parent.TryGetComponentInParent(out Container _)) state |= CS.InContainer; 

                else state |= CS.Removed;

            }

            return state;
        }
    }
}
global using static SCPlus.CarryableUniqueSetup;
using Il2CppTLD.ModularElectrolizer;
using UnityEngine;

namespace SCPlus
{
    internal class CarryableUniqueSetup
    {
        public static IEnumerator PrepareMillingMachine(GameObject go)
        {
            go.name = "INTERACTIVE_IndustrialMillingMachine";
            SimpleInteraction si = go.AddComponent<SimpleInteraction>();
            si.m_DefaultHoverText = new LocalizedString() { m_LocalizationID = "GAMEPLAY_MillingMachine" };

            MillingMachine mm = go.AddComponent<MillingMachine>();
            mm.m_IdleAudioPlay = "Play_MillingMachine";
            mm.m_IdleAudioStop = "Stop_MillingMachine";
            mm.m_RepairAudioLoop = "Play_MillingMachineRepair";

            Action action = () => mm.InitializeInteraction(si);
            InvokableCall invokable = new InvokableCall(action);
            si.FindOrAddEventForEventType(InteractionEventType.InitializeInteraction).AddCall(invokable);
            si.FindOrAddEventForEventType(InteractionEventType.InitializeInteraction).m_Calls.AddListener(invokable);

            action = () => mm.PerformInteraction();
            invokable = new InvokableCall(action);
            si.FindOrAddEventForEventType(InteractionEventType.PerformInteraction).AddCall(invokable);
            si.FindOrAddEventForEventType(InteractionEventType.PerformInteraction).m_Calls.AddListener(invokable);

            yield return new WaitForEndOfFrame();

            mm.InitializeInteraction(si);

            AuroraModularElectrolizer ame = go.GetComponent<AuroraModularElectrolizer>();
            if (ame)
            {
                ame.Initialize();
                ame.InitializeFlickerSet();
                ame.Awake();
            }
            yield break;
        }

        public static GameObject ReconstructStoveWood()
        {
            GameObject go = new GameObject("INTERACTIVE_StoveWoodC");
            go.active = false;
            GameObject mesh = AssetHelper.SafeInstantiateAssetAsync("Assets/ArtAssets/Env/Objects/OBJ_StoveWoodC/OBJ_StoveWoodCMain.FBX", go.transform).WaitForCompletion();
            GameObject stove = new GameObject("Stove");
            stove.transform.parent = go.transform;
            GameObject hinge = new GameObject("Hinge");
            hinge.transform.parent = stove.transform;
            hinge.transform.localPosition = new Vector3(0.3374f, 0.4804f, -0.3461f);
            GameObject door = AssetHelper.SafeInstantiateAssetAsync("Assets/ArtAssets/Env/Objects/OBJ_StoveWoodC/OBJ_StoveWoodCDoor.FBX", hinge.transform).WaitForCompletion();

            MeshCollider collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = AssetHelper.SafeLoadAssetAsync<Mesh>("Assets/ArtAssets/Env/Objects/OBJ_StoveWoodC/OBJ_StoveWoodCMain_Col.FBX").WaitForCompletion();

            // materials

            Material material = AssetHelper.SafeLoadAssetAsync<Material>("Assets/ArtAssets/Env/Objects/Materials/OBJ_StoveWoodB_Dif.mat").WaitForCompletion();

            Renderer[] renderers = [mesh.GetComponent<MeshRenderer>(), door.GetComponent<MeshRenderer>()];
            foreach (Renderer r in renderers)
            {
                r.sharedMaterial = material;
            }

            // animation

            iTweenEvent iteOpen = hinge.AddComponent<iTweenEvent>();
            iteOpen.Initialize("open", -0.083f);
            iTweenEvent iteClose = hinge.AddComponent<iTweenEvent>();
            iteClose.Initialize("close", 0.083f);

            ObjectAnim oa = stove.AddComponent<ObjectAnim>();
            oa.Initialize(hinge);

            // fire

            GameObject fireDonor = AssetHelper.SafeInstantiateAssetAsync("Assets/Prefabs/Interactive/INTERACTIVE_PotBellyStove.prefab").WaitForCompletion();
            Transform fireRoot = GameObject.Instantiate(fireDonor.transform.Find("Fire"), stove.transform);
            fireRoot.name = "Fire";
            //fireRoot.parent = stove.transform;
            GameObject cookingPointIndicator = fireDonor.transform.Find("PlacePoints/Cylinder").gameObject;
            //cookingPointIndicator.transform.parent = go.transform;
            cookingPointIndicator.transform.localPosition = Vector3.zero;

            fireRoot.localPosition = new Vector3(0.06f, 0f, 0.001f);
            Vector3 firePosition = new Vector3(0, 0.2f, -0.2f);
            foreach (Transform t in fireRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLower().StartsWith("fx_stage"))
                {
                    t.localPosition = firePosition;
                    t.localScale = new Vector3(2f, 0.1f, 1f);
                    t.Find("Flame core 2")?.gameObject.SetActive(false);
                }
            }

            Fire fire = fireRoot.GetComponent<Fire>();

            // light

            Transform fxLighting = fireRoot.transform.Find("FX_Lighting");
            MelonCoroutines.Start(SetupStoveLight(fxLighting));

            // components

            WoodStove ws = stove.AddComponent<WoodStove>();
            ws.m_StoveCloseSound = "Play_SndMechWoodstove02Close";
            ws.m_StoveOpenSound = "Play_SndMechWoodstove02Open";
            ws.Fire = fire;
            ws.m_ObjectAnim = oa;
            ws.m_DefaultHoverText = new LocalizedString() { m_LocalizationID = "GAMEPLAY_WoodStove" };

            ActiveBurner ab = mesh.AddComponent<ActiveBurner>();
            ab.m_Fire = fire;
            //ab.m_Rend = mesh.GetComponent<Renderer>(); // handled by ab.Start()
            //ab.m_Material = ab.m_Rend.sharedMaterial;
            ab.m_OpacityRange = new MinMax(0f, 1f);

            // cooking slots

            Transform placePointRoot = new GameObject("PlacePoints").transform;
            placePointRoot.parent = go.transform;

            GameObject pointLeft = new GameObject("Left");
            pointLeft.transform.parent = placePointRoot;
            pointLeft.transform.localPosition = new Vector3(-0.217f, 0.8275f, -0.09f);
            GameObject pointRight = new GameObject("Right");
            pointRight.transform.parent = placePointRoot;
            pointRight.transform.localPosition = new Vector3(0.205f, 0.8275f, -0.085f);

            GameObject[] points = [pointLeft, pointRight];
            List<CookingSlot> css = new();
            List<Renderer> indicators = new();

            foreach (GameObject p in points)
            {
                GearPlacePoint gpp = p.AddComponent<GearPlacePoint>();
                gpp.m_AllowedGear = new();
                gpp.m_AllowAllCookingItems = true;
                gpp.m_FireToAttach = fire;

                GameObject indicator = GameObject.Instantiate(cookingPointIndicator, p.transform);
                indicator.name = "Indicator";
                indicator.transform.localScale = new Vector3(0.2451f, 0.0026f, 0.2451f);

                CookingSlot slot = indicator.GetComponent<CookingSlot>();
                slot.m_GearPlacePoint = gpp;
                slot.SetFireplaceHost(ws);

                css.Add(slot);
                indicators.Add(indicator.GetComponent<Renderer>());
            }

            GameObject.Destroy(cookingPointIndicator);
            GameObject.Destroy(fireDonor);

            PlacePoints pp = placePointRoot.gameObject.AddComponent<PlacePoints>();
            pp.m_PlacePoints = indicators.ToArray();

            ws.m_CookingSlots = css.ToArray();
            ws.enabled = true;

            //GameObject goa = Resources.Load<GameObject>("Assets/PrefabInstance/INTERACTIVE_StoveWoodC_LOD0");

            go.active = true;

            return go;
        }

        public static IEnumerator SetupStoveLight(Transform lightRoot)
        {
            yield return new WaitForEndOfFrame();

            lightRoot.localPosition = Vector3.zero;
            Light cookieLight = lightRoot.GetChild(0).GetComponent<Light>();
            Light staticLight = lightRoot.GetChild(1).GetComponent<Light>();
            LightDancing animatedLight1 = lightRoot.GetChild(2).GetComponent<LightDancing>();
            LightDancing animatedLight2 = lightRoot.GetChild(3).GetComponent<LightDancing>();
            staticLight.range = 0.5f;
            staticLight.transform.localPosition = Vector3.up * 0.45f;
            //animatedLight1.Update();
            animatedLight1.originalPosition = new Vector3(-0.05f, 0.4725f, -0.3f);
            animatedLight1.shiftRate = 4f;
            animatedLight1.GetComponent<LightFadeFire>().originalBrightness = 2.5f;
            //animatedLight2.Update();
            animatedLight2.originalPosition = new Vector3(0.05f, 0.4725f, -0.3f);
            animatedLight2.shiftRate = 3f;
            animatedLight2.GetComponent<LightFadeFire>().originalBrightness = 1f;
            animatedLight2.GetComponent<Light>().range = 5f;

            cookieLight.transform.localPosition = new Vector3(-0.0029f, 0.4737f, -0.338f);
            cookieLight.cookie = AssetHelper.SafeLoadAssetAsync<Texture2D>("Assets/ArtAssets/Textures/FX/FX_LightCookieA_Dif.tga").WaitForCompletion();
            cookieLight.spotAngle = 100f;
            cookieLight.innerSpotAngle = 80f;

            yield break;
        }

        public static GameObject ReconstructTunnelLantern()
        {
            string name = "INTERACTIVE_TunnelLantern_Prefab";
            GameObject go = UnityEngine.Object.Instantiate(CarryableData.sneakyBundle.LoadAsset<GameObject>(name));
            go.name = name;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r.name.ToLower().Contains("glass")) r.sharedMaterial.shader = transparentShader;
                else r.sharedMaterial.shader = standardShader;
                r.castShadows = false;
            }

            GameObject interaction = new ("InteractLightsource");
            interaction.transform.parent = go.transform;

            GameObject lightRoot = new GameObject("LightRoot");
            lightRoot.transform.parent = go.transform;
            lightRoot.transform.localPosition = new Vector3(0f, 0.14f, 0.12f);
            lightRoot.active = false;
            GameObject light = new GameObject("Light");
            light.transform.parent = lightRoot.transform;
            light.transform.localPosition = Vector3.zero;

            var l = light.AddComponent<Light>();
            light.AddComponent<LightTracking>();
            l.cookie = AssetHelper.SafeLoadAssetAsync<Cubemap>("Assets/ArtAssets/Textures/FX/FX_LightCookieLantern.psd").WaitForCompletion();
            l.intensity = 2.2f; // vanilla 4, lower for balance™
            l.range = 6f; // vanilla 10, lower for balance™
            l.color = new Color(1f, 0.4906f, 0f);
            l.shadowBias = 0.01f;
            l.shadows = LightShadows.Hard;

            GameObject particleDonor = UnityEngine.Object.Instantiate(GearItem.LoadGearItemPrefab("GEAR_KeroseneLampB").gameObject);
            GameObject p = particleDonor.transform.Find("FX/LampFlame").gameObject;
            p.transform.parent.gameObject.active = true;
            p.transform.parent = lightRoot.transform;
            p.transform.localPosition = Vector3.up * 0.05f;

            UnityEngine.Object.Destroy(p.GetComponent<ParticleRandomEmission>());
            var ps = p.GetComponent<ParticleSystem>();
            ps.emissionRate = 50f;
            ps.maxParticles = 20;
            ps.startSize = 0.05f;
            ps.startSpeed = 0.1f;
            ps.startLifetime = 0.6f;

            UnityEngine.Object.Destroy(particleDonor);

            var il = interaction.AddComponent<InteractiveLightsource>();
            il.m_ExtinguishLightProgress = new LocalizedString() { m_LocalizationID = "GAMEPLAY_Extinguishing" };
            il.m_StartingLightProgress = new LocalizedString() { m_LocalizationID = "GAMEPLAY_LanternLighting" };
            il.m_LightPromptText = new LocalizedString() { m_LocalizationID = "GAMEPLAY_Light" };
            il.m_ExtinguishPromptText = new LocalizedString() { m_LocalizationID = "GAMEPLAY_Extinguish" };
            il.m_ExtinguishFireGameTimeMinutes = 1f;
            il.m_ExtinguishFireRealTimeSeconds = 2f;
            il.m_StartFireGameTimeMinutes = 1f;
            il.m_StartFireRealTimeSeconds = 2f;
            il.m_Light = lightRoot;
            il.m_LightType = LightSourceType.TunnelLantern;
            il.m_LightsourceLinks = new();
            il.m_MissionIllumination = null;

            var si = interaction.AddComponent<SimpleInteraction>();
            si.AddEventCallback(InteractionEventType.PerformInteraction, (UnityAction<BaseInteraction>)(_ => il.PerformInteraction()));
            si.m_DefaultHoverText.m_LocalizationID = "GAMEPLAY_WhiteoutListLantern";
            si.InitializeInteraction();

            var ft = interaction.AddComponent<SCPlusSimpleFuelTank>();
            ft.Init(SCPlusSimpleFuelTank.fuelTankUser.tunnelLantern, 1);

            return go;
        }

        public static GameObject ReconstructTraderRadio()
        {
            GameObject go = new GameObject("INTERACTIVE_SURVIVAL_TraderRadio");
            GameObject mesh = AssetHelper.SafeInstantiateAssetAsync("Assets/ArtAssets/Characters/NPC/Props/Survival_TraderRadio.prefab", go.transform).WaitForCompletion();
            mesh.transform.parent = go.transform;

            BoxCollider c = go.AddComponent<BoxCollider>();
            c.center = new Vector3(0f, 0.085f, 0f);
            c.size = new Vector3(0.7f, 0.17f, 0.4f);

            return go;
        }
    }
}

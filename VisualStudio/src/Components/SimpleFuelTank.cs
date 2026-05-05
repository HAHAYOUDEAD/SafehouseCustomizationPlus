using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppInterop.Runtime;
using Il2CppTLD.Gear;

namespace SCPlus
{
    [RegisterTypeInIl2Cpp]
    internal class SCPlusSimpleFuelTank(IntPtr intPtr) : MonoBehaviour(intPtr)
    {
        public enum fuelTankUser
        {
            undefined,
            tunnelLantern
        }

        public fuelTankUser type = fuelTankUser.undefined;
        public int burnTimeHours = 1;

        private LiquidItem? fuel;
        private bool isActive = false;
        private bool empty;
        private Component? driver;
        private int tick = -1;

        void Awake()
        {
            this.tick = GetTick();
            this.fuel = this.GetOrAddComponent<LiquidItem>();
        }

        public void Init(fuelTankUser type, int burnTimeHours)
        {
            this.type = type;
            this.burnTimeHours = burnTimeHours;
            switch (type)
            {
                case fuelTankUser.tunnelLantern:
                    SetupTunnelLantern();
                    break;
            }
        }

        private void SetupTunnelLantern()
        {
            this.fuel.m_LiquidType = LiquidType.m_Kerosene;
            this.fuel.m_LiquidCapacity = new(500000000); // 0.5 liters
            this.fuel.m_Liquid = this.empty ? new(0) : this.fuel.m_LiquidCapacity;
            this.fuel.m_AmountPerUseVolume = new(8250000 / burnTimeHours); // 8250000 ~ 60 ingame minutes
            this.driver = this.GetComponentInParent<InteractiveLightsource>();
        }

        public void Refuel()
        {
            var pm = GameManager.GetPlayerManagerComponent();
            long availableUnits = pm.GetTotalLiters(this.fuel.m_LiquidType).m_Units;

            if (this.fuel.m_LiquidCapacity.m_Units - this.fuel.m_Liquid.m_Units < this.fuel.m_LiquidCapacity.m_Units * 0.1f) // over 90%
            {
                //GameManager.GetPlayerVoiceComponent().Play("Play_VOCatchBreath", Il2CppVoice.Priority.Low);
                DialogueSay(Localization.Get("SCP_FuelTank_Full"), 8f);
                return;
            }

            if (availableUnits < 1)
            {
                //HUDMessage.AddMessage(Localization.Get("SCP_FuelTank_RefuelFailed"), true, true);
                //GameManager.GetPlayerVoiceComponent().Play("Play_FailGeneralSwitch", Il2CppVoice.Priority.Low); // damn in/come on
                DialogueSay(Localization.Get("SCP_FuelTank_RefuelFailed"), 8f);
                GameAudioManager.PlayGUIError();
                return;
            }


            IntPtr methodPtr = IL2CPP.il2cpp_class_get_method_from_name( // lmao
                Il2CppClassPointerStore<SCPlusSimpleFuelTank>.NativeClassPtr,
                "RefuelComplete",
                0 // parameter count
            );

            var @delegate = new OnExitDelegate(this, methodPtr);

            InterfaceManager.GetPanel<Panel_GenericProgressBar>().Launch(
                Localization.Get("GAMEPLAY_RefuelingProgress"), 
                6f, // real seconds
                1f, // game time minutes
                0f, // failure threshold
                "Play_SndActionRefuelLantern", // audio name
                null, // voice name
                false, // suppress heavy breathing
                true, // skip restore in hands
                @delegate
             );

        }

        public void TurnOff()
        {
            if (this.driver == null) return;
            switch (type)
            {
                case fuelTankUser.tunnelLantern:
                    TurnOffTunnelLantern();
                    break;
            }

        }

        void TurnOffTunnelLantern()
        {
            var ils = driver.TryCast<InteractiveLightsource>();
            if (ils)
            {
                ils.SetState(false);
            }
        }

        public void RefuelComplete()
        {
            var pm = GameManager.GetPlayerManagerComponent();
            long availableUnits = pm.GetTotalLiters(this.fuel.m_LiquidType).m_Units;

            ItemLiquidVolume availableCapacity = new(this.fuel.m_LiquidCapacity.m_Units - this.fuel.m_Liquid.m_Units);
            long unitsToTransfer = Math.Min(availableUnits, availableCapacity.m_Units);

            this.fuel.AddLiquid(this.fuel.m_LiquidType, new(unitsToTransfer), 0f);
            pm.DeductLiquidFromInventory(new(unitsToTransfer), this.fuel.LiquidType);
            if (this.fuel.m_Liquid.ToQuantity(1f) > 0f) this.empty = false;
        }

        int GetTick()
        {
            float time = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
            return Mathf.FloorToInt(time * 60f); // 1 tick per ingame minute
        }

        void Update()
        {
            switch (this.type)
            {
                case fuelTankUser.tunnelLantern:
                    UpdateTunnelLantern();
                    break;
            }
            if (this.isActive)
            {
                if (this.fuel.m_Liquid.ToQuantity(1f) > 0f)
                {
                    int minute = GetTick();

                    if (minute == tick) return;
                    else
                    {
                        DoTick();
                        this.tick = minute;
                    }
                }
                else
                {
                    this.empty = true;
                }
            }
        }

        void UpdateTunnelLantern()
        {
            if (this.driver == null) return;
            var ils = driver.TryCast<InteractiveLightsource>();
            if (ils)
            {
                

                this.isActive = ils.gameObject.activeInHierarchy && ils.m_IsOn;

                if (this.empty && this.isActive)
                {
                    ils.SetState(false);
                }

                TunnelLanternSwitchPrompt(ils.m_IsOn);
            }
        }

        void TunnelLanternSwitchPrompt(bool isOn)
        {
            this.TryGetComponent<SimpleInteraction>(out var si);

            if (si)
            {
                si.m_EventEntries.RemoveAt(si.m_EventEntries.Count - 1); // remove last
                si.AddEventCallback(InteractionEventType.InitializeInteraction, (UnityAction<BaseInteraction>)(_ => 
                    DisplayInteractionButtons(true, isOn ? "GAMEPLAY_Extinguish" : "GAMEPLAY_Light", "GAMEPLAY_Refuel")));
            }
        }

        public string GetRemainingFuelTimeProcessedString()
        {
            if (this.fuel.m_Liquid.m_Units < 1) return Localization.Get("SCP_FuelTank_Empty");

            float remainingHours = this.fuel.m_Liquid.ToQuantity(1f) / this.fuel.m_AmountPerUseVolume.ToQuantity(1f) / 60f;

            int hours = Mathf.FloorToInt(remainingHours);
            //int minutes = Mathf.FloorToInt((remainingHours - hours) * 60f);
            if (hours <= 1)
            {
                return Localization.Get("SCP_FuelTank_RemainingBurnTime_Low");
            }
            if (hours < 3)
            {
                return Localization.Get("SCP_FuelTank_RemainingBurnTime_Uncertain");
            }

            string l = Localization.Get("SCP_FuelTank_RemainingBurnTime");
            l = l.Replace("{hours}", hours.ToString());
            return l;
        }


        void DoTick() => this.fuel.RemoveLiquid(this.fuel.m_AmountPerUseVolume, out var _);
        public long GetRemainingFuelUnits() => this.fuel.m_Liquid.m_Units;
        public void SetRemainingFuelUnits(long units)
        {
            if (units < 0) units = 0;
            if (units > this.fuel.m_LiquidCapacity.m_Units) units = this.fuel.m_LiquidCapacity.m_Units;
            if (units == 0) this.empty = true;
            else this.empty = false;
            this.fuel.m_Liquid = new(units);
        }
    }
}

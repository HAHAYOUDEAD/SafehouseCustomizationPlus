using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace SCPlus
{
   
    internal class CarriedObjectSaveDataProxy
    {
        public CarriedObjectData.CarriedObjectType type;
        public AssetReference assetRef = new AssetReference();
        public string originalScene = "";
        public string currentScene = ""; // if DontDestroyOnLoad = in player inv
        public Vector3 originalPos;
        public Vector3 currentPos;
        public Quaternion currentRot;
        //public Vector3 currentScale;
    }

    internal class CarriedObjectData
    {
        public static float carriedObjectWeight = 0f;
        public enum CarriedObjectType
        { 
            Basic,
            Stove,
            WaterSource,
            TraderRadio,
            TreeLimb,
            Forge,
            Workbench
        
        }
    }
}

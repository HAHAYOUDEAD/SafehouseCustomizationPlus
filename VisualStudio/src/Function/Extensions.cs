using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCPlus
{
    public static class Extensions
    {
        public static bool TryGetGuid(this Container c, out string guid)
        {
            if (c.GetComponent<ObjectGuid>())
            {
                guid = c.GetComponent<ObjectGuid>().PDID;
                return true;
            }

            guid = missingGuid;
            return false;
        }

        public static string JsonDumpSkipDefaults<T>(this T obj) // thanks GPT
        {
            if (obj == null) return "null";

            var type = typeof(T);
            var properties = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            StringBuilder jsonBuilder = new StringBuilder();
            jsonBuilder.Append('{');

            bool firstProperty = true;

            jsonBuilder.Append($"\"@type\":{JSON.Dump(type.FullName)},");

            foreach (var prop in properties)
            {
                object value = prop.GetValue(obj);
                object defaultValue = prop.FieldType.GetDefaultValue();

                // Skip default values and empyt strings
                if (value == null || value.Equals(defaultValue)) continue;
                if (prop.FieldType == typeof(string) && value?.Equals(string.Empty) == true) continue;
                //if (prop.FieldType == typeof(int) && value?.Equals(-1) == true) continue;

                if (!firstProperty) jsonBuilder.Append(',');
                firstProperty = false;

                jsonBuilder.Append($"\"{prop.Name}\":{JSON.Dump(value)}");
            }

            jsonBuilder.Append('}');
            return jsonBuilder.ToString();
        }

        private static object GetDefaultValue(this Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;

        public static Color HueAdjust(this Color c, float hue)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            return Color.HSVToRGB(hue, s, v);
        }

        public static Color AlphaAdjust(this Color c, float alpha)
        {
            return new Color(c.r, c.g, c.b, alpha);
        }

        public static void MakeEmpty(this Container c)
        {
            c.m_Inspected = true;
            c.m_DisableSerialization = false;
            c.m_RolledSpawnChance = true;
            c.m_NotPopulated = false;
            c.m_StartHasBeenCalled = true;
            c.m_StartInspected = true;
            c.m_GearToInstantiate.Clear();

            if (c.TryGetComponent(out Lock l))
            {
                l.SetLockState(LockState.Unlocked);
                l.m_LockStateRolled = true;
            }
        }

        public static iTweenEvent Initialize(this iTweenEvent ite, string name, float rotation, float speed = 1f)
        {
            ite.animating = false;
            ite.playAutomatically = false;
            ite.tweenName = name;
            ite.type = iTweenEvent.TweenType.RotateBy;
            ite.vector3s = new Vector3[] { new Vector3(0, rotation, 0) };
            ite.floats = new float[] { speed };
            ite.keys = new string[] { "time", "amount" };
            ite.indexes = new int[] { 0, 0 };

            return ite;
        }

        public static ObjectAnim Initialize(this ObjectAnim oa, GameObject target)
        {
            oa.m_Target = target;
            oa.Start();

            return oa;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
//using System.Text.RegularExpressions;

namespace ROEngines
{
    public static class ROEExtensions
    {
        
        #region SSTU Extensions for Deployable Engines

        public static Transform[] FindChildren(this Transform transform, String name)
        {
            List<Transform> trs = new List<Transform>();
            if (transform.name == name) { trs.Add(transform); }
            locateTransformsRecursive(transform, name, trs);
            return trs.ToArray();
        }

        private static void locateTransformsRecursive(Transform tr, String name, List<Transform> output)
        {
            foreach (Transform child in tr)
            {
                if (child.name == name) { output.Add(child); }
                locateTransformsRecursive(child, name, output);
            }
        }

        public static Transform FindRecursive(this Transform transform, String name)
        {
            if (transform.name == name) { return transform; }//was the original input transform
            Transform tr = transform.Find(name);//found as a direct child
            if (tr != null) { return tr; }
            foreach (Transform child in transform)
            {
                tr = child.FindRecursive(name);
                if (tr != null) { return tr; }
            }
            return null;
        }

        public static string GetStringValue(this ConfigNode node, String name, String defaultValue)
        {
            String value = node.GetValue(name);
            return value == null ? defaultValue : value;
        }

        public static string GetStringValue(this ConfigNode node, String name)
        {
            return GetStringValue(node, name, "");
        }

        public static bool GetBoolValue(this ConfigNode node, String name, bool defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return bool.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static float GetFloatValue(this ConfigNode node, String name, float defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return float.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static int GetIntValue(this ConfigNode node, String name, int defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return int.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static int GetIntValue(this ConfigNode node, String name)
        {
            return GetIntValue(node, name, 0);
        }

        #endregion

        #region SSTU Extensions for SRB's
        /*

        public static FloatCurve GetFloatCurve(this ConfigNode node, String name, FloatCurve defaultValue = null)
        {
            FloatCurve curve = new FloatCurve();
            if (node.HasNode(name))
            {
                ConfigNode curveNode = node.GetNode(name);
                String[] values = curveNode.GetValues("key");
                int len = values.Length;
                String[] splitValue;
                float a, b, c, d;
                for (int i = 0; i < len; i++)
                {
                    splitValue = Regex.Replace(values[i], @"\s+", " ").Split(' ');
                    if (splitValue.Length > 2)
                    {
                        a = ROEUtil.safeParseFloat(splitValue[0]);
                        b = ROEUtil.safeParseFloat(splitValue[1]);
                        c = ROEUtil.safeParseFloat(splitValue[2]);
                        d = ROEUtil.safeParseFloat(splitValue[3]);
                        curve.Add(a, b, c, d);
                    }
                    else
                    {
                        a = ROEUtil.safeParseFloat(splitValue[0]);
                        b = ROEUtil.safeParseFloat(splitValue[1]);
                        curve.Add(a, b);
                    }
                }
            }
            else if (defaultValue != null)
            {
                foreach (Keyframe f in defaultValue.Curve.keys)
                {
                    curve.Add(f.time, f.value, f.inTangent, f.outTangent);
                }
            }
            else
            {
                curve.Add(0, 0);
                curve.Add(1, 1);
            }
            return curve;
        }

        public static ConfigNode getNode(this FloatCurve curve, string name)
        {
            ConfigNode node = new ConfigNode(name);
            int len = curve.Curve.length;
            Keyframe[] keys = curve.Curve.keys;
            for (int i = 0; i < len; i++)
            {
                Keyframe key = keys[i];
                node.AddValue("key", key.time + " " + key.value + " " + key.inTangent + " " + key.outTangent);
            }
            return node;
        }

        public static void loadSingleLine(this FloatCurve curve, string input)
        {
            string[] keySplits = input.Split(':');
            string[] valSplits;
            int len = keySplits.Length;
            float key, value, inTan, outTan;
            for (int i = 0; i < len; i++)
            {
                valSplits = keySplits[i].Split(',');
                key = float.Parse(valSplits[0]);
                value = float.Parse(valSplits[1]);
                inTan = float.Parse(valSplits[2]);
                outTan = float.Parse(valSplits[3]);
                curve.Add(key, value, inTan, outTan);
            }
        }

        public static string ToStringSingleLine(this FloatCurve curve)
        {
            string data = "";
            int len = curve.Curve.length;
            Keyframe key;
            for (int i = 0; i < len; i++)
            {
                key = curve.Curve.keys[i];
                if (i > 0) { data = data + ":"; }
                data = data + key.time + "," + key.value + "," + key.inTangent + "," + key.outTangent;
            }
            return data;
        } 

        public static bool[] GetBoolValues(this ConfigNode node, String name)
        {
            String[] values = node.GetValues(name);
            int len = values.Length;
            bool[] vals = new bool[len];
            for (int i = 0; i < len; i++)
            {
                vals[i] = ROEUtil.safeParseBool(values[i]);
            }
            return vals;
        }

        public static float[] GetFloatValues(this ConfigNode node, String name, float[] defaults)
        {
            String baseVal = node.GetStringValue(name);
            if (!String.IsNullOrEmpty(baseVal))
            {
                String[] split = baseVal.Split(new char[] { ',' });
                float[] vals = new float[split.Length];
                for (int i = 0; i < split.Length; i++) { vals[i] = ROEUtil.safeParseFloat(split[i]); }
                return vals;
            }
            return defaults;
        }

        public static float[] GetFloatValues(this ConfigNode node, String name)
        {
            return GetFloatValues(node, name, new float[] { });
        }

        public static float GetFloatValue(this ConfigNode node, String name)
        {
            return GetFloatValue(node, name, 0);
        }

        /// <summary>
        /// Performs the input delegate onto the input part module and any modules found in symmetry counerparts.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="module"></param>
        /// <param name="action"></param>
        public static void actionWithSymmetry<T>(this T module, Action<T> action) where T : PartModule
        {
            action(module);
            forEachSymmetryCounterpart(module, action);
        }

        /// <summary>
        /// Performs the input delegate onto any modules found in symmetry counerparts. (does not effect this.module)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="module"></param>
        /// <param name="action"></param>
        public static void forEachSymmetryCounterpart<T>(this T module, Action<T> action) where T : PartModule
        {
            int index = module.part.Modules.IndexOf(module);
            int len = module.part.symmetryCounterparts.Count;
            for (int i = 0; i < len; i++)
            {
                action((T)module.part.symmetryCounterparts[i].Modules[index]);
            }
        }

        */
        #endregion
    }
}

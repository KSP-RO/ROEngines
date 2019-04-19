using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ROEngines
{
    public class ROEUtil
    {
        #region SSTU Utilities for Deployable Engines

        public static ConfigNode parseConfigNode(String input)
        {
            ConfigNode baseCfn = ConfigNode.Parse(input);
            if (baseCfn == null) { MonoBehaviour.print("ERROR: Base config node was null!!\n" + input); }
            else if (baseCfn.nodes.Count <= 0) { MonoBehaviour.print("ERROR: Base config node has no nodes!!\n" + input); }
            return baseCfn.nodes[0];
        }

        #endregion

        #region SSTU Utilities for SRB's
        /*
        public static String printFloatCurve(FloatCurve curve)
        {
            string str = "";
            if (curve != null)
            {
                int len = curve.Curve.length;
                Keyframe key;
                for (int i = 0; i < len; i++)
                {
                    if (i > 0) { str = str + "\n"; }
                    key = curve.Curve.keys[i];
                    str = str + key.time + "," + key.value + "," + key.inTangent + "," + key.outTangent;
                }
            }
            return str;
        }
        public static float safeParseFloat(String val)
        {
            float returnVal = 0;
            try
            {
                returnVal = float.Parse(val);
            }
            catch (Exception e)
            {
                MonoBehaviour.print("ERROR: could not parse float value from: '" + val + "'\n" + e.Message);
            }
            return returnVal;
        }

        internal static bool safeParseBool(string v)
        {
            if (v == null) { return false; }
            else if (v.Equals("true") || v.Equals("yes") || v.Equals("1")) { return true; }
            return false;
        }
        */
        #endregion

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ROEngines
{
    public static class ROELog
    {
        public static readonly bool debugMode = true;

        public static void stacktrace()
        {
            MonoBehaviour.print(System.Environment.StackTrace);
        }

        public static void log(string line)
        {
            MonoBehaviour.print("ROE-LOG  : " + line);
        }

        public static void log(System.Object obj)
        {
            MonoBehaviour.print("ROE-LOG  : " + obj);
        }

        public static void error(string line)
        {
            MonoBehaviour.print("ROE-ERROR: " + line);
        }

        public static void debug(string line)
        {
            if (!debugMode) { return; }
            MonoBehaviour.print("ROE-DEBUG: " + line);
        }

        public static void debug(object obj)
        {
            if (!debugMode) { return; }
            MonoBehaviour.print(obj);
        }

    }
}

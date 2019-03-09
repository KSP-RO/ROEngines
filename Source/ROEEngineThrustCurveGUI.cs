using System;
using System.Collections;
using System.Linq;
using System.Text;
using RealFuels;
using SolverEngines;
using UnityEngine;

namespace ROEngines
{

    /// <summary>
    /// Part-module responsible for managing of custom thrust curves for SRBs.<para/>
    /// Should be placed in the part -after- any ModuleEngines that are to be manipulated.
    /// This is a (almost) straight copy from SSTU and ShadowMage. The original code was released under the GNU license.
    /// </summary>
    public class ROEEngineThrustCurveGUI : PartModule
    {
        /// <summary>
        /// Current curve preset name.  Will be either the name of a preset curve or 'custom' if a user-defined curve is in use.
        /// </summary>
        [KSPField(isPersistant =true)]
        public string presetCurveName = string.Empty;

        /// <summary>
        /// True/false if using a preset or custom curve.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool usePresetCurve = true;

        /// <summary>
        /// Serialized text version of the current user-custom curve.  Only populated if a preset curve is not in use.
        /// </summary>
        [KSPField(isPersistant = true)]
        private string customCurveData = string.Empty;

        /// <summary>
        /// The actual curve data in use.  This will be a copy of a preset curve, or the run-time representation of a user-defined curve.
        /// </summary>
        [KSPField(isPersistant = true)]
        private FloatCurve currentCurve;

        /// <summary>
        /// Tracks if init has been done on this part
        /// </summary>
        private bool initialized = false;


        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Thrust Curve")]
        public string thrustCurveName;

        [KSPField(isPersistant = true)]
        private string savedCurve = string.Empty;


        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Open Thrust Curve Editor")]
        public void openThrustCurveGUI()
        {
            ThrustCurveEditorGUI.openGUI(this, presetCurveName, currentCurve);
        }
        
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            /*
            if (node.HasNode("currentCurve"))
            {
                ROELog.debug("currentCurve: " + node.GetFloatCurve("currentCurve"));
                currentCurve = new FloatCurve();
                currentCurve = node.GetFloatCurve("currentCurve");
                thrustCurveName = node.GetStringValue("thrustCurveName");
            }
            */
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsEditor)
            {
                StartCoroutine(RunLateStart());
            }
            initialize();
        }

        public void Start()
        {
            if (!HighLogic.LoadedSceneIsEditor)
            {
                StartCoroutine(RunLateStart());
            }
            //updateEngineCurve();
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            //node.AddValue("savedCurve", savedCurve.ToStringSingleLine());
            //ROELog.debug("savedCurve stored: " + savedCurve.ToStringSingleLine());
        }

        private IEnumerator RunLateStart()
        {
            yield return new WaitForSeconds(1.0f);
            updateEngineCurve();
        }

        /// <summary>
        /// Initializes this module.  Loads custom curve data from persistence if necessary, and updates the engine module with the currently loaded curve.
        /// </summary>
        private void initialize()
        {
            if (initialized) { return; }
            initialized = true;
            ROELog.debug("Initializing...");

            if (currentCurve == null)
            {
                ROELog.debug("currentCurve is null");
                if (!string.IsNullOrEmpty(customCurveData))
                {
                    ROELog.debug("Using customCurveData");
                    //load currentCurve from customCurveData
                    currentCurve = new FloatCurve();
                    currentCurve.loadSingleLine(customCurveData);
                    thrustCurveName = part.partInfo.title + " Custom";
                }
                else if (usePresetCurve && !string.IsNullOrEmpty(presetCurveName))
                {
                    ROELog.debug("Using presetCurve: " + presetCurveName);
                    //load currentCurve from PresetCurve data
                    loadPresetCurve(presetCurveName);
                    thrustCurveName = presetCurveName;
                    customCurveData = "";
                }
                else
                {
                    ROELog.debug("Using curve from existing Engine Module");
                    //init to the curve that exists in the engine already
                    ModuleEnginesRF pm = part.Modules.GetModule<ModuleEnginesRF>();
                    usePresetCurve = true;
                    thrustCurveName = part.partInfo.title + " Curve";
                    currentCurve = new FloatCurve();
                    currentCurve.loadSingleLine(pm.thrustCurve.ToStringSingleLine());
                }
            }
            updateEngineCurve();
        }

        public void thrustCurveGuiClosed(string preset, FloatCurve curve)
        {
            //update the persistent curve data from
            currentCurve = curve;
            presetCurveName = preset;
            usePresetCurve = !string.IsNullOrEmpty(presetCurveName);
            thrustCurveName = presetCurveName;
            if (!usePresetCurve)
            {
                customCurveData = currentCurve.ToStringSingleLine();
                thrustCurveName = part.partInfo.title + " Custom";
            }
            ROELog.debug("Updating engine thrust curve data.  Use preset: " + usePresetCurve);
            updateEngineCurve();
        }

        /// <summary>
        /// Applies the 'currentCurve' to the engine module as its active thrust curve.
        /// </summary>

        private void updateEngineCurve()
        {
            ROELog.debug("Updating engine curve");
            if (currentCurve == null) // Code Error
            {
                ROELog.error("currentCurve is null");
                return;
            }

            else
            {
                foreach (ModuleEnginesRF eng in part.FindModulesImplementing<ModuleEnginesRF>())
                {
                    ROELog.debug("Engine curve set to: " + thrustCurveName);
                    eng.thrustCurve = currentCurve;
                    ROELog.debug("New Curve: " + eng.thrustCurve.ToStringSingleLine());
                }
            }
        }

        private void loadPresetCurve(string presetName)
        {
            ConfigNode[] presetNodes = GameDatabase.Instance.GetConfigNodes("ROE_THRUSTCURVE");
            ThrustCurvePreset preset;
            int len = presetNodes.Length;
            for (int i = 0; i < len; i++)
            {
                if (presetNodes[i].GetStringValue("name") == presetName)
                {
                    preset = new ThrustCurvePreset(presetNodes[i]);
                    currentCurve = preset.curve;
                    thrustCurveName = preset.name;
                    updateEngineCurve();
                    break;
                }
            }
        }
    }
}

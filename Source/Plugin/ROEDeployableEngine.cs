using System.Linq;

namespace ROLib.Modules
{
    /// <summary>
    /// This is the module that allows engines (like the RL10-B2) to deploy. This code is orignally written by
    /// ShadowMage as part of the SSTU Mod. It has been adapted to work with the ROEngines code.
    /// </summary>
    public class ROEDeployableEngine : PartModule
    {
        /// <summary>
        /// engine ID for the engine module that this deployable engine module is responsible for
        /// </summary>
        [KSPField] public string engineID = "Engine";
        [KSPField(isPersistant = true)] public string persistentState = ROLAnimState.STOPPED_START.ToString();

        [Persistent] public string configNodeData = string.Empty;

        private bool initialized = false;
        private ROLAnimationModule animationModule;
        private ModuleEnginesFX engineModule;
        
        [KSPAction("Activate Engine")]
        public void DeployEngineAction(KSPActionParam _) => DeployEngineEvent();

        [KSPAction("Shutdown Engine")]
        public void RetractEngineAction(KSPActionParam _) => RetractEngineEvent();

        [KSPEvent(name = "DeployEngineEvent", guiName = "Activate Engine", guiActive = true)]
        public void DeployEngineEvent() => animationModule.onDeployEvent();

        [KSPEvent(name = "RetractEngineEvent", guiName = "Shutdown Engine", guiActive = true)]
        public void RetractEngineEvent()
        {
            if (engineModule && engineModule.EngineIgnited)
                engineModule.Shutdown();
            animationModule.onRetractEvent();
        }

        public void Update() => animationModule?.Update();

        public void OnAnimationStateChange(ROLAnimState newState)
        {
            if (newState == ROLAnimState.STOPPED_END && HighLogic.LoadedSceneIsFlight && engineModule)
                engineModule.Activate();
        }
        
        public override void OnActive()
        {
            if (animationModule.animState == ROLAnimState.STOPPED_END)
            {
                engineModule?.Activate();
            }
            else
            {
                DeployEngineEvent();
                if (engineModule && engineModule.EngineIgnited)
                {
                    engineModule.Shutdown();
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            if (string.IsNullOrEmpty(configNodeData)) configNodeData = node.ToString();
        }

        public override void OnStart(StartState state) => Initialize();

        public override void OnStartFinished(StartState state)
        {
            engineModule = part.GetComponents<ModuleEnginesFX>().Where(x => x.engineID == engineID).FirstOrDefault();
            if (engineModule == null)
            {
                engineModule = part.GetComponents<ModuleEnginesFX>().FirstOrDefault();
                ROLLog.error($"ROLDeployableEngine.OnStartFinished(): Could not locate engine by ID: {engineID} on part {part}.  This will cause errors during gameplay.  Trying default: {engineModule}");
            }
            SetupEngineModuleGui();
        }

        private void Initialize()
        {
            if (initialized) { return; }
            initialized = true;
            ConfigNode node = ROLUtils.parseConfigNode(configNodeData);
            AnimationData animData = new AnimationData(node.GetNode("ANIMATIONDATA"));
            animationModule = new ROLAnimationModule(part, this, nameof(persistentState), null, nameof(DeployEngineEvent), nameof(RetractEngineEvent));
            animationModule.getSymmetryModule = m => ((ROEDeployableEngine)m).animationModule;
            animationModule.setupAnimations(animData, part.transform.ROLFindRecursive("model"), 0);
            animationModule.onAnimStateChangeCallback = OnAnimationStateChange;
        }

        private void SetupEngineModuleGui()
        {
            if (engineModule is ModuleEnginesFX)
            {
                engineModule.Events[nameof(engineModule.Activate)].active = false;
                engineModule.Events[nameof(engineModule.Shutdown)].active = false;
                engineModule.Events[nameof(engineModule.Activate)].guiActive = false;
                engineModule.Events[nameof(engineModule.Shutdown)].guiActive = false;
                engineModule.Actions[nameof(engineModule.ActivateAction)].active = false;
                engineModule.Actions[nameof(engineModule.ShutdownAction)].active = false;
                engineModule.Actions[nameof(engineModule.OnAction)].active = false;
            }
        }
    }
}

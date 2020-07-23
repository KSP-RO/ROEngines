using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using ROLib;
using KSPShaderTools;


namespace ROEngines
{
	// TODO: Create a proper Effects modifier for RealPlume
	/// <summary>
    /// PartModule that allows for highly customizable SRB boosters that can range from Separators to Segmented Boosters. This module also allows the players to customize the nose and mount attachments.
    /// </summary>
    public class ROEModularSRB : PartModule, IPartCostModifier, IPartMassModifier, IRecolorable
    {
        #region KSPFields

        [KSPField]
        public float diameterLargeStep = 0.1f;

        [KSPField]
        public float diameterSmallStep = 0.1f;

        [KSPField]
        public float diameterSlideStep = 0.001f;

        [KSPField]
        public float minDiameter = 0.08f;

        [KSPField]
        public float maxDiameter = 7.0f;

        [KSPField]
        public float volumeScalingPower = 3f;

        [KSPField]
        public float massScalingPower = 3f;

        [KSPField]
        public bool enableVScale = true;

        [KSPField]
        public int coreContainerIndex = 0;

        [KSPField]
        public int noseContainerIndex = 0;

        [KSPField]
        public int mountContainerIndex = 0;

        [KSPField]
        public string coreManagedNodes = string.Empty;

        [KSPField]
        public string noseManagedNodes = string.Empty;

        [KSPField]
        public string mountManagedNodes = string.Empty;

        [KSPField]
        public string noseInterstageNode = "noseinterstage";

        [KSPField]
        public string mountInterstageNode = "mountinterstage";

        [KSPField]
        public float actualHeight = 0.0f;


        #region RCS Fields

        [KSPField]
        public int lowerRCSIndex = 0;

        [KSPField]
        public string lowerRCSThrustTransform = "RCSThrustTransform";

        [KSPField]
        public string lowerRCSFunctionSource = "LOWERRCS";

        [KSPField]
        public string currentLowerRCS = "default";

        [KSPField]
        public string currentLowerRCSLayout = "default";

        [KSPField]
        public string currentLowerRCSParent = "MOUNT";

        [KSPField]
        public float currentLowerRCSOffset = 0.0f;

        #endregion RCS Fields


        #region Engine Fields

        [KSPField]
        public float thrustScalingPower = 3f;

        [KSPField]
        public string engineThrustTransform = "thrustTransform";

        [KSPField]
        public string gimbalTransform = "gimbalTransform";

        [KSPField]
        public string engineTransformSource = "CORE";

        [KSPField]
        public string engineThrustSource = "CORE";

        #endregion Engine Fields

        #region SRB Fields

        [KSPField]
        public float maxThrust = 0.0f;


        #endregion SRB Fields



        #region UI Fields

        /// <summary>
        /// This is the total length of the entire tank with the nose, core and mounts all considered.
        /// </summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Total Length", guiFormat = "F4", guiUnits = "m")]
        public float totalTankLength = 0.0f;

        /// <summary>
        /// This is the largest diameter of the entire tank.
        /// </summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Largest Diameter", guiFormat = "F4", guiUnits = "m")]
        public float largestDiameter = 0.0f;

        /// <summary>
        /// The current user selected diamater of the part.  Drives the scaling and positioning of everything else in the model.
        /// </summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Diameter", guiUnits = "m"),
         UI_FloatEdit(sigFigs = 4, suppressEditorShipModified = true)]
        public float currentDiameter = 1.0f;

        /// <summary>
        /// Adjustment to the vertical-scale of v-scale compatible models/module-slots.
        /// </summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "V.ScaleAdj"),
         UI_FloatEdit(sigFigs = 4, suppressEditorShipModified = true, minValue = -1, maxValue = 1, incrementLarge = 0.25f, incrementSmall = 0.05f, incrementSlide = 0.001f)]
        public float currentVScale = 0f;

        /// <summary>
        /// Does the SRB have Attitude Control and what type?
        /// </summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Att.Control"),
            UI_ChooseOption(suppressEditorShipModified = true)]
        public string attitudeControl = "None";

        /// <summary>
        /// What material is the case made out of?
        /// </summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Case Mat."),
            UI_ChooseOption(suppressEditorShipModified = true)]
        public string caseMaterial = "Steel";

        /// <summary>
        /// What propellant does it use?
        /// </summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Case Mat."),
            UI_ChooseOption(suppressEditorShipModified = true)]
        public string propellantType = "PSPC";


        #region Persistent Fields

        //------------------------------------------MODEL SELECTION SET PERSISTENCE-----------------------------------------------//

        //non-persistent value; initialized to whatever the currently selected core model definition is at time of loading
        //allows for variant names to be updated in the part-config without breaking everything....
        [KSPField(isPersistant = true, guiName = "Variant", guiActiveEditor = true, guiActive = false),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentVariant = "Default";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Nose"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentNose = "Mount-None";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Core"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentCore = "Mount-None";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Nozzle"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentMount = "Mount-None";

        //------------------------------------------TEXTURE SET PERSISTENCE-----------------------------------------------//

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Nose Tex"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentNoseTexture = "default";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Core Tex"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentCoreTexture = "default";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Nozzle Tex"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentMountTexture = "default";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "RCS Tex"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentLowerRCSTexture = "default";

        //------------------------------------------RECOLORING PERSISTENCE-----------------------------------------------//

        //persistent data for modules; stores colors
        [KSPField(isPersistant = true)]
        public string noseModulePersistentData = string.Empty;

        [KSPField(isPersistant = true)]
        public string coreModulePersistentData = string.Empty;

        [KSPField(isPersistant = true)]
        public string mountModulePersistentData = string.Empty;

        [KSPField(isPersistant = true)]
        public string lowerRCSModulePersistentData = string.Empty;

        // Tracks if default textures and resource volumes have been initialized; only occurs once during the parts' first Start() call
        [KSPField(isPersistant = true)]
        public bool initializedDefaults = false;

        #endregion Persistent Fields

        #endregion UI Fields

        #endregion KSPFields


        #region Private Variables

        /// <summary>
        /// Standard work-around for lack of config-node data being passed consistently and lack of support for mod-added serializable classes.
        /// </summary>
        [Persistent]
        public string configNodeData = string.Empty;

        /// <summary>
        /// Has initialization been run?  Set to true the first time init methods are run (OnLoad/OnStart), and ensures that init is only run a single time.
        /// </summary>
        private bool initialized = false;

        /// <summary>
        /// The adjusted mass for this part.
        /// </summary>
        private float modifiedMass = -1;

        /// <summary>
        /// The adjusted cost for this part.
        /// </summary>
        private float modifiedCost = -1;

        /// <summary>
        /// The radius values used for positioning the RCS module. These values are updated in the UpdatePositions method.
        /// </summary>
        private float lowerRCSRad;

        /// <summary>
        /// Previous diameter value, used for surface attach position updates.
        /// </summary>
        private float prevDiameter = -1;

        private string[] noseNodeNames;
        private string[] coreNodeNames;
        private string[] mountNodeNames;

        //Main module slots for nose/core/mount/rcs
        private ROLModelModule<ROEModularSRB> noseModule;
        private ROLModelModule<ROEModularSRB> coreModule;
        private ROLModelModule<ROEModularSRB> mountModule;
        private ROLModelModule<ROEModularSRB> lowerRCSModule;

        /// <summary>
        /// Mapping of all of the variant sets available for this part.  When variant list length > 0, an additional 'variant' UI slider is added to allow for switching between variants.
        /// </summary>
        private Dictionary<string, ModelDefinitionVariantSet> variantSets = new Dictionary<string, ModelDefinitionVariantSet>();

        /// <summary>
        /// Helper method to get or create a variant set for the input variant name.  If no set currently exists, a new set is empty set is created and returned.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private ModelDefinitionVariantSet getVariantSet(string name)
        {
            ModelDefinitionVariantSet set = null;
            if (!variantSets.TryGetValue(name, out set))
            {
                set = new ModelDefinitionVariantSet(name);
                variantSets.Add(name, set);
            }
            return set;
        }

        /// <summary>
        /// Helper method to find the variant set for the input model definition.  Will nullref/error if no variant set is found.  Will NOT create a new set if not found.
        /// </summary>
        /// <param name="def"></param>
        /// <returns></returns>
        private ModelDefinitionVariantSet getVariantSet(ModelDefinitionLayoutOptions def)
        {
            //returns the first variant set out of all variants where the variants definitions contains the input definition
            return variantSets.Values.Where((a, b) => { return a.definitions.Contains(def); }).First();
        }

        #endregion Private Variables

        #region Standard Overrides

        // Standard KSP lifecyle override
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (string.IsNullOrEmpty(configNodeData)) { configNodeData = node.ToString(); }
            initialize();
        }

        // Standard KSP lifecyle override
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            initialize();
            initializeUI();
            updateDimensions();
        }

        // Standard Unity lifecyle override
        public void Start()
        {
            initializedDefaults = true;
            updateRCSModule();
            updateEngineModule();
            updateDragCubes();
        }

        // Standard Unity lifecyle override
        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Remove(new EventData<ShipConstruct>.OnEvent(onEditorVesselModified));
            }
        }

        // KSP editor modified event callback
        private void onEditorVesselModified(ShipConstruct ship)
        {
            //update available variants for attach node changes
            updateAvailableVariants();
        }

		// IPartMassModifier Override
		public ModifierChangeWhen GetModuleMassChangeWhen()
        { return ModifierChangeWhen.CONSTANTLY; }

        // IPartMassModifier Override
        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            if (modifiedMass == -1) { return 0; }
            return -defaultMass + modifiedMass;
        }

        // IPartCostModifier Override
        public ModifierChangeWhen GetModuleCostChangeWhen()
        { return ModifierChangeWhen.CONSTANTLY; }

        // IPartCostModifier Override
        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            if (modifiedCost == -1) { return 0; }
            return -defaultCost + modifiedCost;
        }

        // IRecolorable Override
        public string[] getSectionNames()
        {
            return new string[] { "Nose", "Core", "Mount", "LowerRCS" };
        }

        // IRecolorable Override
        public RecoloringData[] getSectionColors(string section)
        {
            if (section == "Nose")
            {
                return noseModule.recoloringData;
            }
            else if (section == "Core")
            {
                return coreModule.recoloringData;
            }
            else if (section == "Mount")
            {
                return mountModule.recoloringData;
            }
            else if (section == "LowerRCS")
            {
                return lowerRCSModule.recoloringData;
            }
            return coreModule.recoloringData;
        }

        // IRecolorable Override
        public void setSectionColors(string section, RecoloringData[] colors)
        {
            if (section == "Nose")
            {
                noseModule.setSectionColors(colors);
            }
            else if (section == "Core")
            {
                coreModule.setSectionColors(colors);
            }
            else if (section == "Mount")
            {
                mountModule.setSectionColors(colors);
            }
            else if (section == "LowerRCS")
            {
                lowerRCSModule.setSectionColors(colors);
            }
        }

        // IRecolorable Override
        public TextureSet getSectionTexture(string section)
        {
            if (section == "Nose")
            {
                return noseModule.textureSet;
            }
            else if (section == "Core")
            {
                return coreModule.textureSet;
            }
            else if (section == "Mount")
            {
                return mountModule.textureSet;
            }
            else if (section == "LowerRCS")
            {
                return lowerRCSModule.textureSet;
            }
            return coreModule.textureSet;
        }

        // IContainerVolumeContributor override
        public ContainerContribution[] getContainerContributions()
        {
            ContainerContribution[] cts;
            ContainerContribution ct0 = getCC("nose", noseContainerIndex, noseModule.moduleVolume * 1000f);
            ContainerContribution ct1 = getCC("core", coreContainerIndex, coreModule.moduleVolume * 1000f);
            ContainerContribution ct2 = getCC("mount", mountContainerIndex, mountModule.moduleVolume * 1000f);
            cts = new ContainerContribution[3] { ct0, ct1, ct2 };
            return cts;
        }

        private ContainerContribution getCC(string name, int index, float vol)
        {
            float contVol = vol;
            return new ContainerContribution(name, index, contVol);
        }

        #endregion Standard Overrides

        #region Custom Update Methods

        /// <summary>
        /// Initialization method.  Sets up model modules, loads their configs from the input config node.  Does all initial linking of part-modules.<para/>
        /// Does NOT set up their UI interaction -- that is all handled during OnStart()
        /// </summary>
        private void initialize()
        {
            if (initialized) { return; }
            initialized = true;

            prevDiameter = currentDiameter;

            noseNodeNames = ROLUtils.parseCSV(noseManagedNodes);
            coreNodeNames = ROLUtils.parseCSV(coreManagedNodes);
            mountNodeNames = ROLUtils.parseCSV(mountManagedNodes);

            //model-module setup/initialization
            ConfigNode node = ROLConfigNodeUtils.parseConfigNode(configNodeData);

            //list of CORE model nodes from config
            //each one may contain multiple 'model=modelDefinitionName' entries
            //but must contain no more than a single 'variant' entry.
            //if no variant is specified, they are added to the 'Default' variant.
            ConfigNode[] coreDefNodes = node.GetNodes("CORE");
            ModelDefinitionLayoutOptions[] coreDefs;
            List<ModelDefinitionLayoutOptions> coreDefList = new List<ModelDefinitionLayoutOptions>();
            int coreDefLen = coreDefNodes.Length;
            for (int i = 0; i < coreDefLen; i++)
            {
                string variantName = coreDefNodes[i].ROLGetStringValue("variant", "Default");
                coreDefs = ROLModelData.getModelDefinitionLayouts(coreDefNodes[i].ROLGetStringValues("model"));
                coreDefList.AddUniqueRange(coreDefs);
                ModelDefinitionVariantSet mdvs = getVariantSet(variantName);
                mdvs.addModels(coreDefs);
            }
            coreDefs = coreDefList.ToArray();

            //model defs - brought here so we can capture the array rather than the config node+method call
            ModelDefinitionLayoutOptions[] noseDefs = ROLModelData.getModelDefinitions(node.GetNodes("NOSE"));
            ModelDefinitionLayoutOptions[] mountDefs = ROLModelData.getModelDefinitions(node.GetNodes("MOUNT"));
            ModelDefinitionLayoutOptions[] rcsDnDefs = ROLModelData.getModelDefinitions(node.GetNodes("LOWERRCS"));

            noseModule = new ROLModelModule<ROEModularSRB>(part, this, getRootTransform("ROEModularSRB-NOSE"), ModelOrientation.TOP, nameof(currentNose), null, nameof(currentNoseTexture), nameof(noseModulePersistentData));
            noseModule.name = "ROEModularSRB-Nose";
            noseModule.getSymmetryModule = m => m.noseModule;
            noseModule.getValidOptions = () => noseDefs;

            coreModule = new ROLModelModule<ROEModularSRB>(part, this, getRootTransform("ROEModularSRB-CORE"), ModelOrientation.CENTRAL, nameof(currentCore), null, nameof(currentCoreTexture), nameof(coreModulePersistentData));
            coreModule.name = "ROEModularSRB-Core";
            coreModule.getSymmetryModule = m => m.coreModule;
            coreModule.getValidOptions = () => getVariantSet(currentVariant).definitions;

            mountModule = new ROLModelModule<ROEModularSRB>(part, this, getRootTransform("ROEModularSRB-MOUNT"), ModelOrientation.BOTTOM, nameof(currentMount), null, nameof(currentMountTexture), nameof(mountModulePersistentData));
            mountModule.name = "ROEModularSRB-Mount";
            mountModule.getSymmetryModule = m => m.mountModule;
            mountModule.getValidOptions = () => mountDefs;

            lowerRCSModule = new ROLModelModule<ROEModularSRB>(part, this, getRootTransform("ROEModularSRB-LOWERRCS"), ModelOrientation.CENTRAL, nameof(currentLowerRCS), null, nameof(currentLowerRCSTexture), nameof(lowerRCSModulePersistentData));
            lowerRCSModule.name = "ROEModularSRB-LowerRCS";
            lowerRCSModule.getSymmetryModule = m => m.lowerRCSModule;
            lowerRCSModule.getValidOptions = () => rcsDnDefs;
            lowerRCSModule.getLayoutPositionScalar = () => lowerRCSRad;
            lowerRCSModule.getLayoutScaleScalar = () => 1f;

            noseModule.volumeScalar = volumeScalingPower;
            coreModule.volumeScalar = volumeScalingPower;
            mountModule.volumeScalar = volumeScalingPower;
            lowerRCSModule.volumeScalar = volumeScalingPower;

            noseModule.massScalar = massScalingPower;
            coreModule.massScalar = massScalingPower;
            mountModule.massScalar = massScalingPower;
            lowerRCSModule.massScalar = massScalingPower;

            // Set up the model lists and load the currently selected model
            noseModule.setupModelList(noseDefs);
            coreModule.setupModelList(coreDefs);
            mountModule.setupModelList(mountDefs);
            mountModule.setupModelList(rcsDnDefs);
            coreModule.setupModel();
            noseModule.setupModel();
            mountModule.setupModel();
            lowerRCSModule.setupModel();

            // Initialize RCS Thrust Transforms
            getModuleByName(lowerRCSFunctionSource).renameRCSThrustTransforms(lowerRCSThrustTransform);

            // Initialize Engine Thrust Transforms and Gimbal
            getModuleByName(engineTransformSource).renameEngineThrustTransforms(engineThrustTransform);
            getModuleByName(engineTransformSource).renameGimbalTransforms(gimbalTransform);

            updateModulePositions();
            updateDimensions();
            updateMassAndCost();
            updateAttachNodes(false);
            updateAvailableVariants();
            ROLStockInterop.updatePartHighlighting(part);
        }

        /// <summary>
        /// Initialize the UI controls, including default values, and specifying delegates for their 'onClick' methods.<para/>
        /// All UI based interaction code will be defined/run through these delegates.
        /// </summary>
        private void initializeUI()
        {
            Action<ROEModularSRB> modelChangedAction = (m) =>
            {
                m.updateModulePositions();
                m.updateDimensions();
                m.updateMassAndCost();
                m.updateAttachNodes(true);
                m.updateAvailableVariants();
                m.updateDragCubes();
                m.updateRCSModule();
                m.updateEngineModule();
                ROLModInterop.updateResourceVolume(m.part);
            };

            //set up the core variant UI control
            string[] variantNames = ROLUtils.getNames(variantSets.Values, m => m.variantName);
            this.ROLupdateUIChooseOptionControl(nameof(currentVariant), variantNames, variantNames, true, currentVariant);
            Fields[nameof(currentVariant)].guiActiveEditor = variantSets.Count > 1;

            Fields[nameof(currentVariant)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                //TODO find variant set for the currently enabled core model
                //query the index from that variant set
                ModelDefinitionVariantSet prevMdvs = getVariantSet(coreModule.definition.name);
                //this is the index of the currently selected model within its variant set
                int previousIndex = prevMdvs.indexOf(coreModule.layoutOptions);
                //grab ref to the current/new variant set
                ModelDefinitionVariantSet mdvs = getVariantSet(currentVariant);
                //and a reference to the model from same index out of the new set ([] call does validation internally for IAOOBE)
                ModelDefinitionLayoutOptions newCoreDef = mdvs[previousIndex];
                //now, call model-selected on the core model to update for the changes, including symmetry counterpart updating.
                this.ROLactionWithSymmetry(m =>
                {
                    m.currentVariant = currentVariant;
                    m.coreModule.modelSelected(newCoreDef.definition.name);
                    modelChangedAction(m);
                });
            };

            Fields[nameof(currentDiameter)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                this.ROLactionWithSymmetry(m =>
                {
                    if (m != this) { m.currentDiameter = this.currentDiameter; }
                    modelChangedAction(m);
                    m.prevDiameter = m.currentDiameter;
                });
                ROLStockInterop.fireEditorUpdate();
            };

            Fields[nameof(currentVScale)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                this.ROLactionWithSymmetry(m =>
                {
                    if (m != this) { m.currentVScale = this.currentVScale; }
                    modelChangedAction(m);
                });
                ROLStockInterop.fireEditorUpdate();
            };

            Fields[nameof(currentNose)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                noseModule.modelSelected(a, b);
                this.ROLactionWithSymmetry(modelChangedAction);
                ROLStockInterop.fireEditorUpdate();
            };

            Fields[nameof(currentCore)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                coreModule.modelSelected(a, b);
                this.ROLactionWithSymmetry(modelChangedAction);
                ROLStockInterop.fireEditorUpdate();
            };

            Fields[nameof(currentMount)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                mountModule.modelSelected(a, b);
                this.ROLactionWithSymmetry(modelChangedAction);
                ROLStockInterop.fireEditorUpdate();
            };

            //------------------MODEL DIAMETER SWITCH UI INIT---------------------//
            if (maxDiameter == minDiameter)
            {
                Fields[nameof(currentDiameter)].guiActiveEditor = false;
            }
            else
            {
                this.ROLupdateUIFloatEditControl(nameof(currentDiameter), minDiameter, maxDiameter, diameterLargeStep, diameterSmallStep, diameterSlideStep, true, currentDiameter);
            }
            Fields[nameof(currentVScale)].guiActiveEditor = enableVScale;

            //------------------MODULE TEXTURE SWITCH UI INIT---------------------//
            Fields[nameof(currentNoseTexture)].uiControlEditor.onFieldChanged = noseModule.textureSetSelected;
            Fields[nameof(currentCoreTexture)].uiControlEditor.onFieldChanged = coreModule.textureSetSelected;
            Fields[nameof(currentMountTexture)].uiControlEditor.onFieldChanged = mountModule.textureSetSelected;
            Fields[nameof(currentLowerRCSTexture)].uiControlEditor.onFieldChanged = lowerRCSModule.textureSetSelected;

            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Add(new EventData<ShipConstruct>.OnEvent(onEditorVesselModified));
            }

            // Force the Textures selection to show up to keep the PAW at the same size
            Fields[nameof(currentNoseTexture)].guiActiveEditor = true;
            Fields[nameof(currentCoreTexture)].guiActiveEditor = true;
            Fields[nameof(currentMountTexture)].guiActiveEditor = true;
            Fields[nameof(currentLowerRCSTexture)].guiActiveEditor = true;
        }

        /// <summary>
        /// Update the scale and position values for all currently configured models.  Does no validation, only updates positions.<para/>
        /// After calling this method, all models will be scaled and positioned according to their internal position/scale values and the orientations/offsets defined in the models.
        /// </summary>
        private void updateModulePositions()
        {
            // Scales for modules depend on the module above/below them
            // First set the scale for the core module -- this depends directly on the UI specified 'diameter' value.
            coreModule.setScaleForDiameter(currentDiameter, currentVScale);

            // Set nose scale values
            noseModule.setDiameterFromBelow(coreModule.moduleUpperDiameter, currentVScale);

            // Set mount scale values
            mountModule.setDiameterFromAbove(coreModule.moduleLowerDiameter, currentVScale);

            //total height of the part is determined by the sum of the heights of the modules at their current scale
            float totalHeight = noseModule.moduleHeight;
            totalHeight += coreModule.moduleHeight;
            totalHeight += mountModule.moduleHeight;

            // Position of each module is set such that the vertical center of the models is at part origin/COM
            float pos = totalHeight * 0.5f;	// Absolute top of model
            pos -= noseModule.moduleHeight;	// Bottom of nose model
            noseModule.setPosition(pos);
            pos -= coreModule.moduleHeight * 0.5f;	// Center of core model
            coreModule.setPosition(pos);
            pos -= coreModule.moduleHeight * 0.5f;	// Bottom of 'core' model
            mountModule.setPosition(pos);

            // Update actual model positions and scales
            noseModule.updateModelMeshes();
            coreModule.updateModelMeshes();
            mountModule.updateModelMeshes();

            // Scale and position of RCS and solar models handled a bit differently
            float coreScale = coreModule.moduleHorizontalScale;

            ROLModelModule<ROEModularSRB> module = getModuleByName(currentLowerRCSParent);
            module.getRCSMountingValues(currentLowerRCSOffset, false, out lowerRCSRad, out pos);
            lowerRCSModule.setScale(coreScale);
            lowerRCSModule.setPosition(pos);
            lowerRCSModule.updateModelMeshes();
            module = getModuleByName(lowerRCSFunctionSource);
            module.renameRCSThrustTransforms(lowerRCSThrustTransform);
        }

        /// <summary>
        /// Updates all dimensions for the PAW and tooling.
        /// </summary>
        private void updateDimensions()
        {
            float noseMaxDiam, mountMaxDiam = 0.0f;
            noseMaxDiam = Math.Max(noseModule.moduleLowerDiameter, noseModule.moduleUpperDiameter);
            ROLLog.debug("currentMount: " + currentMount);
            if (currentMount.Contains("Mount"))
            {
                ROLLog.debug("currentMount: " + currentMount);
                mountMaxDiam = mountModule.moduleUpperDiameter;
            }
            else
                mountMaxDiam = Math.Max(mountModule.moduleLowerDiameter, mountModule.moduleUpperDiameter);

            totalTankLength = getTotalHeight();
            ROLLog.debug("The Total Tank Length is: " + totalTankLength);
            largestDiameter = Math.Max(currentDiameter, Math.Max(noseMaxDiam, mountMaxDiam));
        }

        /// <summary>
        /// Update the cached modifiedMass and modifiedCost field values.  Used with stock cost/mass modifier interface.<para/>
        /// Optionally includes adapter mass/cost if enabled in config.
        /// </summary>
        private void updateMassAndCost()
        {
            modifiedMass = coreModule.moduleMass;
            modifiedMass += lowerRCSModule.moduleMass;
            modifiedMass += noseModule.moduleMass;
            modifiedMass += mountModule.moduleMass;

            modifiedCost = coreModule.moduleCost;
            modifiedCost += lowerRCSModule.moduleCost;
            modifiedCost += noseModule.moduleCost;
            modifiedCost += mountModule.moduleCost;
        }

        /// <summary>
        /// Update the ModuleRCSXX with the current stats for the current configuration (thrust, ISP, fuel type)
        /// </summary>
        private void updateRCSModule()
        {
            ModuleRCS[] rcsModules = part.GetComponents<ModuleRCS>();
            if (rcsModules == null || rcsModules.Length == 0) { return; }//nothing to update
            ROLModelModule<ROEModularSRB> lowerRCSSource = getModuleByName(lowerRCSFunctionSource);
			lowerRCSSource.updateRCSModule(rcsModules[lowerRCSIndex], 3);
        }

        /// <summary>
        /// Update the ModuleEnginesXX with the current stats for the current configuration.
        /// </summary>
        private void updateEngineModule()
        {
            ROLModelModule<ROEModularSRB> engineTransformSource = getModuleByName(this.engineTransformSource);
            engineTransformSource.renameEngineThrustTransforms(engineThrustTransform);
            engineTransformSource.renameGimbalTransforms(gimbalTransform);
            ModuleEngines engine = part.GetComponent<ModuleEngines>();
            if (engine != null)
            {
                ROLModelModule<ROEModularSRB> engineThrustSource = getModuleByName(this.engineThrustSource);
                engineThrustSource.updateEngineModuleThrust(engine, thrustScalingPower);
            }

            //re-init gimbal module
            ModuleGimbal gimbal = part.GetComponent<ModuleGimbal>();
            if (gimbal != null)
            {
                //check to see that gimbal was already initialized
                if ((HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight) && gimbal.gimbalTransforms != null)
                {
                    float range = 0;
                    gimbal.OnStart(StartState.Flying);
                    if (engineTransformSource.definition.engineTransformData != null) { range = engineTransformSource.definition.engineTransformData.gimbalFlightRange; }
                    gimbal.gimbalRange = range;

                    //re-init gimbal offset module if it exists
                    ROEGimbalOffsetModule gOffset = part.GetComponent<ROEGimbalOffsetModule>();
                    if (gOffset != null)
                    {
                        range = 0;
                        if (engineTransformSource.definition.engineTransformData != null)
                        {
                            range = engineTransformSource.definition.engineTransformData.gimbalAdjustmentRange;
                        }
                        gOffset.gimbalXRange = range;
                        gOffset.gimbalZRange = range;
                        gOffset.reInitialize();
                    }
                }
            }

            ROLModelConstraint constraints = part.GetComponent<ROLModelConstraint>();
            if (constraints != null)
            {
                ConfigNode constraintNode;
                if (engineTransformSource.definition.constraintData != null)
                {
                    constraintNode = engineTransformSource.definition.constraintData.constraintNode;
                }
                else
                {
                    constraintNode = new ConfigNode("CONSTRAINT");
                }
                constraints.loadExternalData(constraintNode);
            }
            //updateEffectsScale();
        }

        /// <summary>
        /// Update the attach nodes for the current model-module configuration. 
        /// The 'nose' module is responsible for updating of upper attach nodes, while the 'mount' module is responsible for lower attach nodes.
        /// Also includes updating of 'interstage' nose/mount attach nodes.
        /// Also includes updating of surface-attach node position.
        /// Also includes updating of any parts that are surface attached to this part.
        /// </summary>
        /// <param name="userInput"></param>
        public void updateAttachNodes(bool userInput)
        {
            //update the standard top and bottom attach nodes, using the node position(s) defined in the nose and mount modules
            noseModule.updateAttachNodeTop("top", userInput);
            mountModule.updateAttachNodeBottom("bottom", userInput);

            //update the model-module specific attach nodes, using the per-module node definitions from the part
            noseModule.updateAttachNodeBody(noseNodeNames, userInput);
            coreModule.updateAttachNodeBody(coreNodeNames, userInput);
            mountModule.updateAttachNodeBody(mountNodeNames, userInput);

            // Update the Nose Interstage Node
            float y = noseModule.modulePosition + noseModule.moduleVerticalScale;
            int nodeSize = Mathf.RoundToInt(coreModule.moduleDiameter) + 1;
            Vector3 pos = new Vector3(0, y, 0);
            ROLSelectableNodes.updateNodePosition(part, noseInterstageNode, pos);
            AttachNode noseInterstage = part.FindAttachNode(noseInterstageNode);
            if (noseInterstage != null)
            {
                ROLAttachNodeUtils.updateAttachNodePosition(part, noseInterstage, pos, Vector3.up, userInput, nodeSize);
            }

            // Update the Mount Interstage Node
            y = mountModule.modulePosition + mountModule.moduleVerticalScale;
            nodeSize = Mathf.RoundToInt(coreModule.moduleDiameter) + 1;
            pos = new Vector3(0, y, 0);
            ROLSelectableNodes.updateNodePosition(part, mountInterstageNode, pos);
            AttachNode mountInterstage = part.FindAttachNode(mountInterstageNode);
            if (mountInterstage != null)
            {
                ROLAttachNodeUtils.updateAttachNodePosition(part, mountInterstage, pos, Vector3.down, userInput, nodeSize);
            }            

            //update surface attach node position, part position, and any surface attached children
            AttachNode surfaceNode = part.srfAttachNode;
            if (surfaceNode != null)
            {
                coreModule.updateSurfaceAttachNode(surfaceNode, prevDiameter, userInput);
            }
        }

        /// <summary>
        /// Return the total height of this part in its current configuration.  This will be the distance from the bottom attach node to the top attach node, and may not include any 'extra' structure. TOOLING
        /// </summary>
        /// <returns></returns>
        private float getTotalHeight()
        {
            float totalHeight = noseModule.moduleHeight;
            totalHeight += mountModule.moduleHeight;
            totalHeight += coreModule.moduleHeight;
            return totalHeight;
        }

        /// <summary>
        /// Return the topmost position in the models relative to the part's origin.
        /// </summary>
        /// <returns></returns>
        private float getPartTopY()
        {
            return getTotalHeight() * 0.5f;
        }

        /// <summary>
        /// Update the UI visibility for the currently available selections.<para/>
        /// Will hide/remove UI fields for slots with only a single option (models, textures, layouts).
        /// </summary>
        private void updateAvailableVariants()
        {
            noseModule.updateSelections();
            coreModule.updateSelections();
            mountModule.updateSelections();
            lowerRCSModule.updateSelections();
        }

        /// <summary>
        /// Calls the generic ROT procedural drag-cube updating routines.  Will update the drag cubes for whatever the current model state is.
        /// </summary>
        private void updateDragCubes()
        {
            ROLModInterop.onPartGeometryUpdate(part, true);
        }

        /// <summary>
        /// Return the root transform for the specified name.  If does not exist, will create it and parent it to the parts' 'model' transform.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="recreate"></param>
        /// <returns></returns>
        private Transform getRootTransform(string name)
        {
            Transform root = part.transform.ROLFindRecursive(name);
            if (root != null)
            {
                GameObject.DestroyImmediate(root.gameObject);
                root = null;
            }
            root = new GameObject(name).transform;
            root.NestToParent(part.transform.ROLFindRecursive("model"));
            return root;
        }

        /// <summary>
        /// Return the model-module corresponding to the input slot name.  Valid slot names are: NOSE,UPPER,CORE,LOWER,MOUNT
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private ROLModelModule<ROEModularSRB> getModuleByName(string name)
        {
            switch (name)
            {
                case "NOSE":
                    return noseModule;
                case "CORE":
                    return coreModule;
                case "MOUNT":
                    return mountModule;
                case "LOWERRCS":
                    return lowerRCSModule;
                case "NONE":
                    return null;
                default:
                    return null;
            }
        }

        #endregion Custom Update Methods

        #region Model Definition Storage

        /// <summary>
        /// Data storage for a group of model definitions that share the same 'variant' type.  Used by modular-part in variant-defined configurations.
        /// </summary>
        public class ModelDefinitionVariantSet
        {
            public readonly string variantName;

            public ModelDefinitionLayoutOptions[] definitions = new ModelDefinitionLayoutOptions[0];

            public ModelDefinitionLayoutOptions this[int index]
            {
                get
                {
                    if (index < 0) { index = 0; }
                    if (index >= definitions.Length) { index = definitions.Length - 1; }
                    return definitions[index];
                }
            }

            public ModelDefinitionVariantSet(string name)
            {
                this.variantName = name;
            }

            public void addModels(ModelDefinitionLayoutOptions[] defs)
            {
                List<ModelDefinitionLayoutOptions> allDefs = new List<ModelDefinitionLayoutOptions>();
                allDefs.AddRange(definitions);
                allDefs.AddUniqueRange(defs);
                definitions = allDefs.ToArray();
            }

            public int indexOf(ModelDefinitionLayoutOptions def)
            {
                return definitions.IndexOf(def);
            }

        }

        #endregion Model Definition Storage

    }
}

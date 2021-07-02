using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using KSPShaderTools;
using ROLib;
using static ROLib.ROLLog;
using Debug = UnityEngine.Debug;

namespace ROEngines
{
    [SuppressMessage("ReSharper", "InvertIf")]
    public class ModuleRORCS : PartModule, IPartCostModifier, IPartMassModifier, IRecolorable
    {
        private const string GroupDisplayName = "RO-RCS";
        private const string GroupName = "ModuleRORCS";

        [KSPField] public string rcsThrustTransformName = string.Empty;
        [KSPField] public bool allowRescale = true;
        [KSPField] public float scaleLargeStep = 0.1f;
        [KSPField] public float scaleSmallStep = 0.1f;
        [KSPField] public float scaleSlideStep = 0.001f;
        [KSPField] public float minScale = 0.1f;
        [KSPField] public float maxScale = 100.0f;

        [KSPField(isPersistant = true, guiActiveEditor = true, groupDisplayName = GroupDisplayName, groupName = GroupName, guiName = "Scale"),
         UI_FloatEdit(sigFigs = 3, suppressEditorShipModified = true)]
        public float currentScale = 1f;
        
        [KSPField(isPersistant = true, guiActiveEditor = true, groupName = GroupName, guiName = "Type"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentType = "Default";

        [KSPField(isPersistant = true, guiActiveEditor = true, groupName = GroupName, guiName = "RCS Model"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentRCSModel = "Default";
        
        [KSPField(isPersistant = true, guiActiveEditor = true, groupName = GroupName, guiName = "RCS Texture"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentRCSModelTexture = "Default";
        
        [KSPField(isPersistant = true, guiActiveEditor = true, groupName = GroupName, guiName = "Base"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentBase = "Default";
        
        [KSPField(isPersistant = true, guiActiveEditor = true, groupName = GroupName, guiName = "Base Texture"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentBaseTexture = "Default";
        
        [KSPField(isPersistant = true, guiActiveEditor = true, groupName = GroupName, guiName = "Layout"),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentLayout = "Default";
        
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Thrust", guiFormat = "F4", guiUnits = "kN", groupName = GroupName)]
        public float currentThrust = 0.0f;
        
        [KSPField(isPersistant = true)] public string rcsModelModulePersistentData = string.Empty;
        [KSPField(isPersistant = true)] public string baseModulePersistentData = string.Empty;
        [Persistent] public string configNodeData = string.Empty;
        
        private bool initialized = false;
        private float modifiedMass = -1;
        private float modifiedCost = -1;
        private float rcsThrust, rcsMass, rcsCost;
        internal ROLModelModule<ModuleRORCS> rcsModelModule;
        internal ROLModelModule<ModuleRORCS> baseModule;

        private Transform baseRotatedRoot;
        private Transform baseTransform;
        private Transform rcsModelRotatedRoot;
        private Transform rcsModelTransform;
        private ModuleRCSFX rcsfx;
        
        public ROLDragCubeUpdater dragCubeUpdater;
        
        private readonly Dictionary<string, ModelDefinitionVariantSet> variantSets = new Dictionary<string, ModelDefinitionVariantSet>();
        
        private ModelDefinitionVariantSet GetVariantSet(string name)
        {
            if (!variantSets.TryGetValue(name, out ModelDefinitionVariantSet set))
            {
                set = new ModelDefinitionVariantSet(name);
                variantSets.Add(name, set);
            }
            return set;
        }
        
        internal ModelDefinitionLayoutOptions[] rcsModelDefs;
        internal ModelDefinitionLayoutOptions[] baseDefs;
        
        internal void ModelChangedHandler(bool pushNodes)
        {
            UpdateModelScale();
            UpdateModelMeshes();
            UpdateAttachNodes(pushNodes);
            UpdateAvailableVariants();
            UpdateDragCubes();
            UpdateRCSValues();
            UpdateMass();
            UpdateCost();
            ROLStockInterop.UpdatePartHighlighting(part);
            if (HighLogic.LoadedSceneIsEditor)
                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }

        internal void ModelChangedHandlerWithSymmetry(bool pushNodes, bool symmetry)
        {
            ModelChangedHandler(pushNodes);
            if (symmetry)
            {
                foreach (Part p in part.symmetryCounterparts)
                {
                    p.FindModuleImplementing<ModuleRORCS>().ModelChangedHandler(pushNodes);
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (string.IsNullOrEmpty(configNodeData)) { configNodeData = node.ToString(); }
            Initialize();
        }

        public override void OnStartFinished(StartState state)
        {
            base.OnStartFinished(state);
            Initialize();
            ModelChangedHandler(false);
            InitializeUI();
            rcsfx = part.GetComponent<ModuleRCSFX>();
            UpdateRCSThrust();
        }
        
        private void OnEditorVesselModified(ShipConstruct ship) => UpdateAvailableVariants();

        // IPartMass/CostModifier overrides
        public ModifierChangeWhen GetModuleMassChangeWhen() => ModifierChangeWhen.FIXED;
        public ModifierChangeWhen GetModuleCostChangeWhen() => ModifierChangeWhen.FIXED;
        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit) => Mathf.Max(0, modifiedMass);
        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit) => Mathf.Max(0, modifiedCost);
        
        public string[] getSectionNames() => new string[] { "RCS Model", "Base" };

        public RecoloringData[] getSectionColors(string section)
        {
            return section switch
            {
                "RCS Model" => rcsModelModule.recoloringData,
                "Base" => baseModule.recoloringData,
                _ => rcsModelModule.recoloringData,
            };
        }

        public void setSectionColors(string section, RecoloringData[] colors)
        {
            if (section == "RCS Model") rcsModelModule.setSectionColors(colors);
            else if (section == "Base") baseModule.setSectionColors(colors);
        }

        public TextureSet getSectionTexture(string section)
        {
            return section switch
            {
                "RCS Model" => rcsModelModule.textureSet,
                "Base" => baseModule.textureSet,
                _ => rcsModelModule.textureSet,
            };
        }

        private void Initialize()
        {
            if (initialized) { return; }
            initialized = true;
            
            debug($"Initialize(): dragCubeUpdater");
            dragCubeUpdater = new ROLDragCubeUpdater(part);

            baseRotatedRoot = part.transform.FindRecursive("RORCS-BaseRoot");
            if (baseRotatedRoot == null)
            {
                baseRotatedRoot = new GameObject("RORCS-BaseRoot").transform;
                baseRotatedRoot.NestToParent(part.transform.FindRecursive("model"));
                baseRotatedRoot.Rotate(90, -90, 0);
            }
            
            rcsModelRotatedRoot = part.transform.FindRecursive("RORCS-RCSModelRoot");
            if (rcsModelRotatedRoot == null)
            {
                rcsModelRotatedRoot = new GameObject("RORCS-RCSModelRoot").transform;
                rcsModelRotatedRoot.NestToParent(part.transform.FindRecursive("model"));
                rcsModelRotatedRoot.Rotate(0, 0, 0);
            }
            
            ConfigNode node = ROLConfigNodeUtils.ParseConfigNode(configNodeData);
            
            List<ModelDefinitionLayoutOptions> rcsModelDefList = new List<ModelDefinitionLayoutOptions>();
            foreach (ConfigNode n in node.GetNodes("RCSMODEL"))
            {
                string variantName = n.ROLGetStringValue("variant", "Default");
                rcsModelDefs = ROLModelData.getModelDefinitionLayouts(n.ROLGetStringValues("model"));
                rcsModelDefList.AddUniqueRange(rcsModelDefs);
                ModelDefinitionVariantSet mdvs = GetVariantSet(variantName);
                mdvs.AddModels(rcsModelDefs);
            }
            rcsModelDefs = rcsModelDefList.ToArray();
            baseDefs = ROLModelData.getModelDefinitions(node.GetNodes("RCSBASE"));

            rcsModelTransform = rcsModelRotatedRoot.FindOrCreate("ModuleRORCS-RCSModel");
            rcsModelModule = new ROLModelModule<ModuleRORCS>(part, this, rcsModelTransform, ModelOrientation.CENTRAL, nameof(currentRCSModel), nameof(currentLayout), nameof(currentRCSModelTexture), nameof(rcsModelModulePersistentData))
            {
                name = "ModuleRORCS-RCS",
                getSymmetryModule = m => m.rcsModelModule,
                getValidOptions = () => GetVariantSet(currentType).definitions,
                MassScalar = 3f,
            };
            rcsModelModule.setupModelList(rcsModelDefs);
            rcsModelModule.setupModel();
            rcsModelModule.updateSelections();
            
            baseTransform = baseRotatedRoot.FindOrCreate("ModuleRORCS-BaseModel");
            baseModule = new ROLModelModule<ModuleRORCS>(part, this, baseTransform, ModelOrientation.CENTRAL, nameof(currentBase), nameof(currentLayout), nameof(currentBaseTexture), nameof(baseModulePersistentData))
            {
                name = "ModuleRORCS-Base",
                getSymmetryModule = m => m.baseModule,
                getValidOptions = () => baseDefs,
                MassScalar = 3f,
            };
            baseModule.setupModelList(baseDefs);
            baseModule.setupModel();
            baseModule.updateSelections();

            UpdateModelScale();
            UpdateRCSValues();
            UpdateMass();
            UpdateCost();
            rcsModelModule.RenameRCSThrustTransforms(rcsThrustTransformName);
            UpdateAttachNodes(false);

            var pmlist = part.GetComponents<PartModule>();

            foreach (var pm in pmlist)
            {
                debug($"PM: {pm}");
            }
            
            foreach (var pm in part.Modules)
            {
                debug($"PML: {pm}");
            }
        }

        private void InitializeUI()
        {
            string[] variantNames = ROLUtils.getNames(variantSets.Values, m => m.variantName);
            this.ROLupdateUIChooseOptionControl(nameof(currentType), variantNames, variantNames);
            Fields[nameof(currentType)].guiActiveEditor = variantSets.Count > 1;

            Fields[nameof(currentType)].uiControlEditor.onFieldChanged = (a, b) =>
            {
                ModelDefinitionVariantSet prevMdvs = GetVariantSet(rcsModelModule.definition.name);
                int previousIndex = prevMdvs.IndexOf(rcsModelModule.layoutOptions);
                ModelDefinitionVariantSet mdvs = GetVariantSet(currentType);
                ModelDefinitionLayoutOptions newRCSModelDef = mdvs[previousIndex];
                this.ROLactionWithSymmetry(m =>
                {
                    m.rcsModelModule.modelSelected(newRCSModelDef.definition.name);
                    ModelChangedHandler(true);
                });
                MonoUtilities.RefreshPartContextWindow(part);
            };
            
            Fields[nameof(currentScale)].uiControlEditor.onFieldChanged =
            Fields[nameof(currentScale)].uiControlEditor.onSymmetryFieldChanged = OnModelSelectionChanged;
            
            Fields[nameof(currentRCSModel)].uiControlEditor.onFieldChanged =
            Fields[nameof(currentRCSModel)].uiControlEditor.onSymmetryFieldChanged = OnModelSelectionChanged;
            
            Fields[nameof(currentBase)].uiControlEditor.onFieldChanged =
            Fields[nameof(currentBase)].uiControlEditor.onSymmetryFieldChanged = OnModelSelectionChanged;
            
            this.ROLupdateUIFloatEditControl(nameof(currentScale), minScale, maxScale, scaleLargeStep, scaleSmallStep, scaleSlideStep);

            Fields[nameof(currentScale)].guiActiveEditor = allowRescale;

            //------------------MODULE TEXTURE SWITCH UI INIT---------------------//
            Fields[nameof(currentRCSModelTexture)].uiControlEditor.onFieldChanged = rcsModelModule.textureSetSelected;
            Fields[nameof(currentBaseTexture)].uiControlEditor.onFieldChanged = baseModule.textureSetSelected;

            if (HighLogic.LoadedSceneIsEditor)
                GameEvents.onEditorShipModified.Add(OnEditorVesselModified);
        }

        private void UpdateModelScale()
        {
            debug($"UpdateModelScale()");
            rcsModelModule.SetPosition(0);
            rcsModelModule.SetScale(currentScale);
            rcsModelModule.UpdateModelScalesAndLayoutPositions();
            
            baseModule.RescaleToDiameter(rcsModelModule.moduleDiameter, baseModule.definition.diameter, 0f);
            baseModule.SetPosition(rcsModelModule.ModuleBottom - baseModule.moduleHeight);
            baseModule.UpdateModelScalesAndLayoutPositions();
        }

        private void UpdateRCSValues()
        {
            debug($"UpdateRCSValues()");
            ModelRCSModuleData data = rcsModelModule.layoutOptions.definition.rcsModuleData;
            rcsThrust = data.GetThrust(rcsModelModule.moduleVerticalScale);
            rcsMass = data.GetMass(rcsModelModule.moduleVerticalScale);
            rcsCost = data.GetCost(rcsModelModule.moduleVerticalScale);
        }

        private void UpdateRCSThrust()
        {
            debug($"UpdateRCSThrust()");
            rcsModelModule.RenameRCSThrustTransforms(rcsThrustTransformName);
            if (rcsfx != null)
            {
                rcsfx.thrusterPower = rcsThrust;
            }
        }

        private void UpdateAttachNodes(bool userInput)
        {
            debug($"UpdateAttachNodes()");
            float baseBottomZ = baseModule.ModuleBottom;
            Vector3 pos = new Vector3(-baseBottomZ, 0, 0);
            AttachNode srfNode = part.srfAttachNode;
            if (srfNode != null)
            {
                ROLAttachNodeUtils.UpdateAttachNodePosition(part, srfNode, pos, Vector3.right, userInput, 0);
            }
            AttachNode bottomNode = part.FindAttachNode("bottom");
            if (bottomNode != null)
            {
                ROLAttachNodeUtils.UpdateAttachNodePosition(part, bottomNode, pos, bottomNode.orientation, userInput, 0);
            }
        }
        
        public void UpdateMass()
        {
            debug($"UpdateMass()");
            modifiedMass = rcsMass;
        }

        public void UpdateCost()
        {
            debug($"UpdateCost()");
            modifiedCost = rcsCost;
        }
        
        private void UpdateDragCubes() => dragCubeUpdater.Update();
        
        public void OnModelSelectionChanged(BaseField f, object o)
        {
            debug($"OnModelSelectionChanged()");
            if (f.name == Fields[nameof(currentRCSModel)].name) rcsModelModule.modelSelected(currentRCSModel);
            else if (f.name == Fields[nameof(currentBase)].name) baseModule.modelSelected(currentBase);
            ModelChangedHandler(true);
            MonoUtilities.RefreshPartContextWindow(part);
        }
        
        public void UpdateAvailableVariants()
        {
            debug($"UpdateAvailableVariants()");
            rcsModelModule.updateSelections();
            baseModule.updateSelections();
        }
        
        public void UpdateModelMeshes()
        {
            debug($"UpdateModelMeshes()");
            rcsModelModule.UpdateModelScalesAndLayoutPositions();
            baseModule.UpdateModelScalesAndLayoutPositions();
        }

    }
}
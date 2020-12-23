using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Reflection;

namespace HeavyScience
{

    [KSPAddon(KSPAddon.Startup.Flight, true)]
    internal class StoredData
    {
        internal CelestialBody body;
        internal ExperimentSituations situation;
        internal ScienceSubject subject;
        internal string biomeDisplayName;
        internal string experimentResults;
    };
    

    public class HeavyScienceContainer : PartModule, IPartMassModifier, IScalarModule
    {
        public GameObject sampleObj;
        private float spinRate = 8;
        private float secondsForOneLength = 5;
        public float bounceTop = 0.12f;
        public float bounceBottom = -0.16f;

        private StoredData part_storedexperiment = null;
        private ModuleScienceContainer part_ScienceContainer = null;

        

        [KSPField(isPersistant = true)]
        public string bodyScatterID = "";
        [KSPField(isPersistant = true)]
        public Color ScatterBiomeColor = Color.clear;

        [KSPField(isPersistant = true)]
        public string experimentSubjectID = "";

        [KSPField(isPersistant = true)]
        public bool extraModel = false;

        //[KSPField(isPersistant = true)]
        [KSPField]
        public float sample_mass = (float)Math.Round(Configuration.SurfaceSampleMass,3);

        #region KSPEvents - Control KerbalEVA Interaction
        //Visible EVA only
        [KSPEvent(guiName = "Store Surface Sample", active = true, guiActiveUnfocused = true, externalToEVAOnly = true, unfocusedRange = 5f, name = "storeSurfaceSamplesEvent")]
        public void storeSurfaceSamplesEvent()
        {
            StoreSurfaceSample();
        }
        //Visible EVA only
        [KSPEvent(guiName = "Take Surface Sample", active = false, guiActiveUnfocused = true, externalToEVAOnly = true, unfocusedRange = 5f, name = "takeSurfaceSamplesEvent")]
        public void takeSurfaceSamplesEvent()
        {
            TakeSurfaceSample();
            if (part.parent == null)
                part.vessel.Die();
        }
        #endregion


        
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false)]
        public string Contains = "";


        public void Start()
        {
            //Log.Normal("Running Start!");

            part_ScienceContainer = part.FindModuleImplementing<ModuleScienceContainer>();

            
            part_ScienceContainer.Events["StoreDataExternalEvent"].guiActiveUnfocused = false;
            part_ScienceContainer.Events["CollectDataExternalEvent"].guiActiveUnfocused = false;
            part_ScienceContainer.Events["TransferDataEvent"].guiActiveUnfocused = false;
            part_ScienceContainer.Events["CollectAllEvent"].guiActiveUnfocused = false;
            part_ScienceContainer.Events["ReviewDataEvent"].active = false;
            part_ScienceContainer.Events["ReviewDataEvent"].guiActive = false;

            sampleObj = new GameObject("surfaceSample");
            Transform parenttrans = this.transform.GetChild(0).GetChild(0);
            sampleObj.transform.position = parenttrans.transform.position;
            sampleObj.transform.rotation = parenttrans.transform.rotation;
            sampleObj.transform.SetParent(parenttrans, true);

            secondsForOneLength = Utilities.RandomNumber(3.0f, 7.5f);
            spinRate = Utilities.RandomNumber(5.0f, 8.5f);

            checkforExperimentData();
            if (experimentSubjectID != "")
            {
                if (bodyScatterID == "") 
                    insertSurfaceSample();
                displaySurfaceSample();
            }
        }
        public void OnDestroy()
        {
            //Log.Normal("called");
            Destroy(sampleObj);
        }
        public void Update()
        {
            if (sampleObj != null && bodyScatterID != "")
            {
                sampleObj.transform.Rotate(0, spinRate * TimeWarp.deltaTime, 0);
                sampleObj.transform.localPosition = Vector3.Lerp(new Vector3(0f, this.bounceTop, 0f), new Vector3(0f, this.bounceBottom, 0f),
                  Mathf.SmoothStep(0f, 1f,
                  Mathf.PingPong(Time.time / secondsForOneLength, 1f)
                  ));
            }
            UpdateEventStatus();
        }




        private void insertSurfaceSample()
        {
            //Find scatter with logic for alternatives
            if (!string.IsNullOrEmpty(experimentSubjectID))
            {
                if (experimentSubjectID.Contains("ROCScience_"))
                {
                    Match result = Regex.Match(experimentSubjectID, @"^.*?(?=@)");
                    bodyScatterID = result.Value;
                }
                else if (experimentSubjectID.Contains("cometSample_")) 
                {
                    Log.UserInfo("adding comet_");
                    bodyScatterID = "Comet_Rock";
                }
                else if (experimentSubjectID.Contains("asteroidSample"))
                {
                    Log.UserInfo("adding asteroid_");
                    bodyScatterID = "Potato_Rock";
                }
                else
                {
                    string findBodyBiome = part_storedexperiment.body.name + part_storedexperiment.biomeDisplayName;
                    //Log.UserInfo("Looking for:" + findBodyBiome);
                    if (scatterBuilder.biomeScatterLib.ContainsKey(findBodyBiome))
                    {
                        ScatterBiomeColor = scatterBuilder.biomeScatterLib[findBodyBiome].biomeColor;
                        bool usedAlt = false;
                        if (scatterBuilder.biomeScatterLib[findBodyBiome].AlternateOdds > 0f)
                        {
                            float rollD1000 = ((float)Utilities.RandomNumber(0, 1000)) / 1000.0f;
                            if (rollD1000 < scatterBuilder.biomeScatterLib[findBodyBiome].AlternateOdds)
                            {
                                bodyScatterID = scatterBuilder.biomeScatterLib[findBodyBiome].bodyScatterID_Alt.bodyScatterID;
                                usedAlt = true;
                            }
                        }
                        if (!usedAlt)
                        {
                            extraModel = false;
                            int rando = Utilities.RandomNumber(0, 100);
                            //Log.UserInfo("Storing: " + extraModel + " Minmus: " + scatterBuilder.biomeScatterLib[findBodyBiome].bodyBiome.Contains("Minmus") + " Flats:"+ scatterBuilder.biomeScatterLib[findBodyBiome].bodyBiome.Contains("Flats")  + " Rando:" + rando);
                            if (scatterBuilder.biomeScatterLib[findBodyBiome].bodyBiome.Contains("Minmus") && scatterBuilder.biomeScatterLib[findBodyBiome].bodyBiome.Contains("Flats") && rando >= 50) 
                            {
                                extraModel = true;
                            }

                            bodyScatterID = scatterBuilder.biomeScatterLib[findBodyBiome].bodyScatterID_Default.bodyScatterID;
                            if (part.vessel.mainBody.name == "Bop")
                            {
                                if (Utilities.IsBetween(part.vessel.latitude, 67.5, 68.5) && Utilities.IsBetween(part.vessel.longitude, 116.8, 117.1))
                                {
                                    bodyScatterID = scatterBuilder.biomeScatterLib[findBodyBiome].bodyScatterID_Alt.bodyScatterID;
                                }
                            }

                        }
                    }
                    else
                        Log.UserInfo("Couldn't find bodyBiome:" + findBodyBiome);
                }
            }
        }

        private void displaySurfaceSample()
        {
            //Load Existing (No checks for alternatives)
            if (!string.IsNullOrEmpty(bodyScatterID))
            {
                //bodyBiome tells me how to color the surface sample
                string findBodyBiome = part_storedexperiment.body.name + part_storedexperiment.biomeDisplayName;
                // bodyScatterID tells me what model to show
                if (scatterBuilder.scatterLib.ContainsKey(bodyScatterID))
                {
                    //Log.UserWarning("Displaying: " + bodyScatterID + "  for: " + findBodyBiome);
                    if (scatterBuilder.biomeScatterLib.ContainsKey(findBodyBiome))
                    {
                        if (part_storedexperiment.situation == ExperimentSituations.SrfSplashed || scatterBuilder.biomeScatterLib[findBodyBiome].forceWater)
                        {
                            showwater(scatterBuilder.scatterLib[bodyScatterID], scatterBuilder.biomeScatterLib[findBodyBiome]);
                            return;
                        }
                        else if(scatterBuilder.biomeScatterLib[findBodyBiome].isMini)
                        {
                            showcube(scatterBuilder.scatterLib[bodyScatterID], scatterBuilder.biomeScatterLib[findBodyBiome]);
                            return;
                        }
                    }
                    if (scatterBuilder.scatterLib[bodyScatterID].bodyScatterID == "Kerbin_Grass00")
                    {
                        showgrass(scatterBuilder.scatterLib[bodyScatterID], scatterBuilder.biomeScatterLib[findBodyBiome]);
                    }
                    else
                    {
                        if (scatterBuilder.biomeScatterLib.ContainsKey(findBodyBiome))
                            showrocks(scatterBuilder.scatterLib[bodyScatterID], scatterBuilder.biomeScatterLib[findBodyBiome]);
                        else
                            showrocks(scatterBuilder.scatterLib[bodyScatterID], null);
                        //Log.UserWarning("Scatter: " + scatteritem.bodyScatterID );
                    }
                }
            }
            else
                Log.UserWarning("Scatter bodyScatterID not set!");
        }
        private void showwater(scatterLibrary scatteritem, biomeScatter biomeitem)
        {
            float partscale = part.rescaleFactor * 0.15f;

            GameObject newwater = scatterBuilder.getWaterSphereScatter(biomeitem); //biomeitem.biomeColor
            if (newwater != null)
            {
                newwater.transform.position = sampleObj.transform.position;
                newwater.transform.rotation = sampleObj.transform.rotation;
                newwater.transform.localScale = new Vector3(partscale * 4.5f, partscale * 4.5f, partscale * 4.5f);
                newwater.transform.SetParent(sampleObj.transform, true);

                bounceTop = 0.095f;
                bounceBottom = -0.095f;
            }
        }
        private void showcube(scatterLibrary scatteritem, biomeScatter biomeitem)
        {
            float partscale = part.rescaleFactor * scatteritem.meshScale;

            GameObject newcube= scatterBuilder.getCubeScatter(scatteritem, biomeitem); //biomeitem.biomeColor
            if (newcube != null)
            {
                newcube.transform.position = sampleObj.transform.position;
                newcube.transform.rotation = sampleObj.transform.rotation; // * Quaternion.Euler(-180, 0, 0); ;
                newcube.transform.localScale = new Vector3(partscale, partscale, partscale);

                newcube.transform.SetParent(sampleObj.transform, true);

                bounceTop = -0.6f;
                bounceBottom = -0.65f;
                spinRate = 15;
            }

        }
        private void showrocks(scatterLibrary scatteritem, biomeScatter biomeitem)
        {

            if (biomeitem != null) 
            { 
                //Debug.Log("Displaying: " + biomeitem.bodyBiome + "  using: " + scatteritem.bodyScatterID + "  cone?: " + extraModel + "  VesselPArtCount:(" + part.vessel.parts.Count +")");
            }

            float partscale = part.rescaleFactor * scatteritem.meshScale;
            GameObject newRock = scatterBuilder.getRockScatter(scatteritem, biomeitem, extraModel);
            if (newRock != null)
            {
                newRock.transform.position = sampleObj.transform.position;
                newRock.transform.rotation = sampleObj.transform.rotation;
                newRock.transform.localScale = new Vector3(partscale,  partscale, partscale);
                newRock.transform.SetParent(sampleObj.transform, true);
            }

            bounceTop = scatteritem.bounceUpLimit;
            bounceBottom = scatteritem.bounceDownLimit;
        }
        private void showgrass(scatterLibrary scatteritem, biomeScatter biomeitem)
        {
            GameObject samplemodel = new GameObject("surfaceSample_Grass");
            MeshFilter filterer = samplemodel.AddComponent<MeshFilter>();

            GameObject primPlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh yourMesh = primPlane.GetComponent<MeshFilter>().mesh;
            Destroy(primPlane);

            filterer.sharedMesh = Instantiate(yourMesh);
            MeshRenderer renderer = samplemodel.AddComponent<MeshRenderer>();
            renderer.material = Instantiate(scatteritem.material);

            samplemodel.transform.position = sampleObj.transform.position;
            samplemodel.transform.rotation = sampleObj.transform.rotation;
            samplemodel.transform.SetParent(sampleObj.transform, true);

            //Log.UserInfo("GRASSsamplemodel " + scatteritem.bodyScatterID + " scale: " + scatteritem.meshScale + " Up: " + bounceTop + " Down: " + bounceBottom);
            float partscale = part.rescaleFactor * scatteritem.meshScale;
            samplemodel.transform.localScale = new Vector3(0.001f, partscale, partscale);
            renderer.material.mainTextureScale = new Vector2(scatteritem.materialScale, scatteritem.materialScale);
            bounceTop = scatteritem.bounceUpLimit;
            bounceBottom = scatteritem.bounceDownLimit;

            GameObject leaves2 = Instantiate(samplemodel, sampleObj.transform.position, sampleObj.transform.rotation, sampleObj.transform);
            leaves2.transform.localRotation = Quaternion.Euler(0, 45, 20);
            leaves2.transform.localPosition = new Vector3(0f,-0.02f,0f);

            GameObject leaves3 = Instantiate(samplemodel, sampleObj.transform.position, sampleObj.transform.rotation, sampleObj.transform);
            leaves3.transform.localRotation = Quaternion.Euler(0, 45, -20);
            leaves3.transform.localPosition = new Vector3(0f, -0.02f, 0f);

            GameObject leaves4 = Instantiate(samplemodel, sampleObj.transform.position, sampleObj.transform.rotation, sampleObj.transform);
            leaves4.transform.localRotation = Quaternion.Euler(0, 135, 20);
            leaves4.transform.localPosition = new Vector3(0f, -0.02f, 0f);

            GameObject leaves5 = Instantiate(samplemodel, sampleObj.transform.position, sampleObj.transform.rotation, sampleObj.transform);
            leaves5.transform.localRotation = Quaternion.Euler(0, 135, -20);
            leaves5.transform.localPosition = new Vector3(0f, -0.02f, 0f);
        }

        #region Changing mass processes
        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            if (HighLogic.LoadedSceneIsFlight)
                return ModifierChangeWhen.FIXED;
            else
                return ModifierChangeWhen.STAGED;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            if (bodyScatterID == "")
                return 0;
            return sample_mass;
        }
        #endregion

        #region Science Experiment Lookup
        internal void checkforExperimentData()
        {
            if (part == null)
                return;
            if (part.FindModuleImplementing<HeavyScienceContainer>() == null)
                return;
            if (part_ScienceContainer != null)
            {
                if (part_ScienceContainer.GetStoredDataCount() > 0)
                {
                    foreach (ScienceData scidata in part_ScienceContainer.GetData())
                    {
                        if (!scatterBuilder.BreakingGroundInstalled && scidata.subjectID.Contains("ROCScience"))
                        {
                            Log.Error("Did someone uninstall Breaking Ground DLC? - BG Science report, but no BC Scatter found");
                            return;
                        }
                        part_storedexperiment = GetExperimentData(scidata.subjectID);
                        experimentSubjectID = scidata.subjectID;
                        Fields["Contains"].guiActive = true;
                        Events["takeSurfaceSamplesEvent"].active = true;
                        if (scidata.subjectID.Contains("ROCScience"))
                        {
                            Contains = part_storedexperiment.biomeDisplayName + " ROC Sample";
                            Events["takeSurfaceSamplesEvent"].guiName = "Take " + part_storedexperiment.biomeDisplayName + " ROC Sample";
                        }
                        else
                        {
                            Contains =  part_storedexperiment.biomeDisplayName + " Surface Sample";
                            Events["takeSurfaceSamplesEvent"].guiName = "Take " + part_storedexperiment.biomeDisplayName + " Surface Sample";
                        }
                            
                        return;
                    }
                }
                Fields["Contains"].guiActive = false;
                Events["takeSurfaceSamplesEvent"].active = false;
                Contains = "";
            }
        }

        internal StoredData GetExperimentData(string subjectID)
        {
            StoredData expData = new StoredData();
            string bodyName;
            string biome;

            ScienceUtil.GetExperimentFieldsFromScienceID(subjectID, out bodyName, out expData.situation, out biome);
            expData.body = FlightGlobals.GetBodyByName(bodyName);
            expData.biomeDisplayName = Regex.Replace(ScienceUtil.GetBiomedisplayName(expData.body, biome), " ", ""); 
            expData.experimentResults = ResearchAndDevelopment.GetResults(subjectID);
            expData.subject = ResearchAndDevelopment.GetSubjectByID(subjectID);
            return expData;
        }
        #endregion

        #region iScalar References
        public string ScalarModuleID { get { return ""; } }
        public bool CanMove { get { return true; } }
        public float GetScalar { get { return animationFraction; } }
        public EventData<float, float> OnMoving { get { return new EventData<float, float>("OnMoving"); } }
        public EventData<float> OnStop { get { return new EventData<float>("OnStop"); } }
        public void SetScalar(float t) { animationGoal = t; }
        public bool IsMoving() { return true; }
        public void SetUIWrite(bool value) { }
        public void SetUIRead(bool value) { }
        protected float animationFraction = 0f;
        protected float animationGoal = 0f;
        #endregion

        internal void UpdateEventStatus() 
        {
            if (HighLogic.LoadedSceneIsFlight) 
            {
                if (part_ScienceContainer.GetStoredDataCount() > 0) 
                {
                    Events["storeSurfaceSamplesEvent"].active = false;
                    Events["takeSurfaceSamplesEvent"].active = true;
                    return;
                }

                if (FlightGlobals.ActiveVessel.vesselType == VesselType.EVA)
                {
                    int sameplecount = 0;
                    foreach (var EVADataContainer in FlightGlobals.ActiveVessel.FindPartModulesImplementing<IScienceDataContainer>())
                        foreach (ScienceData StoredSciData in EVADataContainer.GetData())
                            if (Utilities.isSurfaceSample(StoredSciData))
                            {
                                sameplecount++;
                            }
                    if(sameplecount >0)
                        Events["storeSurfaceSamplesEvent"].active = true;
                    else
                        Events["storeSurfaceSamplesEvent"].active = false;
                }
            }
        }

        public void ScienceStored()
        {
            checkforExperimentData();
            insertSurfaceSample();
            displaySurfaceSample();
        }

        public void Scienceremoved()
        {
            bodyScatterID = "";
            experimentSubjectID = "";
            Fields["Contains"].guiActive = false;
            Events["takeSurfaceSamplesEvent"].active = false;
            Contains = "";
            clearSurfaceSample();
        }
        private void clearSurfaceSample()
        {
            foreach (Transform child in sampleObj.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }

        protected void StoreSurfaceSample()
        {
            //I'm assuming that the activevessel is a Kerbal on EVA 
            //Somewhat safe as the Store Surface Sample button only being available while on EVA
            int SampleCount = 0;

            Vessel KerbalonEVA = FlightGlobals.ActiveVessel;
            //Can't use ModuleScienceContainer as the data might still be under the Experiment storage
            foreach (var EVADataContainer in KerbalonEVA.FindPartModulesImplementing<IScienceDataContainer>())
            {
                foreach (ScienceData StoredSciData in EVADataContainer.GetData())
                {
                    if (Utilities.isSurfaceSample(StoredSciData))
                    {
                        Log.UserInfo("Storing Data from EVA: " + StoredSciData.subjectID);
                        if (part_ScienceContainer.AddData(StoredSciData))
                        {
                            EVADataContainer.DumpData(StoredSciData);
                            SampleCount++;

                            StoredData datainfo = GetExperimentData(StoredSciData.subjectID);

                            //Can't store more than 1 Sample
                            //Events["storeSurfaceSamplesEvent"].active = false;

                            //Show button so user can pick up
                            //Events["takeSurfaceSamplesEvent"].active = true;
                            //Events["takeSurfaceSamplesEvent"].guiName = "Take " + datainfo.biomeDisplayName + " Surface Sample";

                            ScienceStored();
                            return;
                        }
                        if (SampleCount != 0) break;

                    }
                }
            }
            if (SampleCount == 0)
                ScreenMessages.PostScreenMessage("No Surface Samples to store.", 4f, ScreenMessageStyle.UPPER_CENTER);

            //Done with checks now do transfer

            ScreenMessages sm = (ScreenMessages)GameObject.FindObjectOfType(typeof(ScreenMessages));
            for (int msgIndex = 0; msgIndex < sm.ActiveMessages.Count(); msgIndex++)
            {
                ScreenMessages.RemoveMessage(sm.ActiveMessages[msgIndex]);
            }
            // Clear Message spam after moving data
        }
        protected void TakeSurfaceSample()
        {
            Vessel KerbalonEVA = FlightGlobals.ActiveVessel;
            ModuleScienceContainer EVADataContainer = KerbalonEVA.FindPartModuleImplementing<ModuleScienceContainer>();

            foreach (ScienceData StoredSciData in part_ScienceContainer.GetData())
            {
                Log.UserInfo("Taking Data from HSContainer: " + StoredSciData.subjectID);
            
                if (EVADataContainer.AddData(StoredSciData))
                {
                    part_ScienceContainer.DumpData(StoredSciData);

                    //Can't store more than 1 Sample
                    Events["storeSurfaceSamplesEvent"].active = true;

                    //Show button so user can pick up
                    Events["takeSurfaceSamplesEvent"].active = false;
                    Events["takeSurfaceSamplesEvent"].guiName = "No Surface Sample Loaded";
                    Scienceremoved();
                }
                else 
                {
                    Log.Error("Transfer fail"); 
                }
            }
        }
    }
}

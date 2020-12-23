using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using KSP.UI.Screens.Flight.Dialogs;
using System.Text.RegularExpressions;


namespace HeavyScience
{
    [KSPAddon(KSPAddon.Startup.Flight, true)]
    public class EVABoarding: PartModule 
    {
        private Part PartBoardingHatch = null;

        private bool BoardPopupShow = false; //show GUI?
        private static GUIStyle PopupWinStyle, PopupLabelStyle, PopupBtnStyle = null;
        private Rect PopupWin = new Rect(500, 500, 200, 155);
        private int PopupWinPosHeight = 100;
        private int PopupWinPosWidth = 100;
        private bool hasInitStyles = false;
        private string BoardingMsgLabeltxt = "";
        private string BoardingMsgHeadingtxt = "";
        private string BoardingMsgButtontxt = "";


        private ModuleScienceContainer StockScienceContainer;
        private float ScreenMsgDelay = 10f;
        private string orangeTxt = "<color=#ff9900ff>";
        [KSPEvent(guiName = "Store Experiments..", active = false, guiActiveUnfocused = false, externalToEVAOnly = true, unfocusedRange = 2f)]
        public void StoreHSDataEvent() { HSStoreScienceData(); }

        [KSPField]
        private bool isBoardingThisPart = false;
        private int Boarding_Countdown = 0;
        

        private KeyCodeExtended BoardKey { get { return GameSettings.EVA_Board.primary; } }

        public void Start()
        {

            if (!hasInitStyles)
            {
                InitWindowStyle();
                InitWindowPoition();
            }
        }


        public void Update()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {

                if (FlightGlobals.ActiveVessel.vesselType == VesselType.EVA)
                {
                    Update_StoreDataX();
                    // Hide popup boarding window if Kerbal moves too far away
                    if (BoardPopupShow) { if (!CrewCanBoard(FlightGlobals.ActiveVessel.evaController)) { BoardPopupShow = false; } }


                    // Boarding needs 2x update ticks before it will allow a container to be emptied properly
                    // Need to change this to an event triggered process
                    if (Boarding_Countdown == 10) { DumpExcessandDupeScienceDataonEVAKerbal(); }
                    if (Boarding_Countdown == 8) { KerbalBoardPart(); }

                    if (Boarding_Countdown == 2) { Boarding_Countdown = 0; }
                    if (Boarding_Countdown > 1) { Boarding_Countdown -= 1; }



                    if (Boarding_Countdown == 1)
                    {
                        DisableBoarding(false);
                        Boarding_Countdown = 0;
                    }

                    

                    if (ExtendedInput.GetKeyUp(BoardKey))
                    {
                        if (part == null) return;
                        KerbalEVA kerbal = FlightGlobals.ActiveVessel.evaController;
                        
                        //Don't do anything if user hit 'B' board key and cannot board
                        if (!CrewCanBoard(kerbal)) { return; }
                        Part partHatch = GetKerbalTargetHatch(kerbal);
                        if (partHatch.craftID  == part.craftID) { isBoardingThisPart = true; }
                        if (isBoardingThisPart)
                        {
                            int SampleStoreResult = 0;
                            if (EVAKerbalhasSurfaceSamples(out int sscount)) { SampleStoreResult = 2; }
                            updateBoardingStatus(SampleStoreResult);
                            if (SampleStoreResult == 0)
                            {
                                Log.UserInfo("BoardingThisPart: " + part.partInfo.title);
                            }
                            else if (SampleStoreResult > 0)
                            {
                                //Log.UserInfo("Still holding SS");
                                PartBoardingHatch = partHatch;
                                DisableBoarding();
                                Boarding_Countdown = 1;
                                BoardPopupShow = true;
                            }
                        }
                        partHatch = null;
                    }
                }
            }
        }



        internal StoredData GetExperimentinfo(string subjectID)
        {
            StoredData expData = new StoredData();
            string bodyName;
            string biome;

            ScienceUtil.GetExperimentFieldsFromScienceID(subjectID, out bodyName, out expData.situation, out biome);
            expData.body = FlightGlobals.GetBodyByName(bodyName);
            expData.biomeDisplayName = ScienceUtil.GetBiomedisplayName(expData.body, biome);
            expData.experimentResults = ResearchAndDevelopment.GetResults(subjectID);
            expData.subject = ResearchAndDevelopment.GetSubjectByID(subjectID);
            return expData;
        }





        #region Boarding FUNCTIONS
        private void onConfirmDumpandBoard()
        {
            if (isBoardingThisPart)
            {
                Boarding_Countdown = 10;
            }
        }
        private void KerbalBoardPart()
        {
            KerbalEVA kerbal = FlightGlobals.ActiveVessel.evaController;
            if (PartBoardingHatch != null)
            {
                kerbal.BoardPart(PartBoardingHatch);
            }
            else
            {
                Log.UserInfo("ERROR: PartBoardingHatch is Null!");
            }
            isBoardingThisPart = false;
            PartBoardingHatch = null;
            updateBoardingStatus(0);

        }


        private bool EVAKerbalhasSurfaceSamples(out int SScount)
        {
            bool hasSurfaceSamples = false;
            SScount = 0;
            if (HighLogic.LoadedSceneIsFlight)
                if (FlightGlobals.ActiveVessel.vesselType == VesselType.EVA)
                    foreach (var EVADataContainer in FlightGlobals.ActiveVessel.FindPartModulesImplementing<IScienceDataContainer>())
                        foreach (ScienceData StoredSciData in EVADataContainer.GetData())
                            
                            if (Utilities.isSurfaceSample(StoredSciData))
                            {
                                SScount++;
                                hasSurfaceSamples = true;
                            } 
            return hasSurfaceSamples;
        }



        private void DumpExcessandDupeScienceDataonEVAKerbal()
        {
            if (HighLogic.LoadedSceneIsFlight)
                if (FlightGlobals.ActiveVessel.vesselType == VesselType.EVA)
                {
                    foreach (var EVADataContainer in FlightGlobals.ActiveVessel.FindPartModulesImplementing<IScienceDataContainer>())
                        foreach (ScienceData StoredSciData in EVADataContainer.GetData())
                            if (Utilities.isSurfaceSample(StoredSciData))
                            {
                                EVADataContainer.DumpData(StoredSciData);
                                Log.UserInfo("Dumped Science Data: " + StoredSciData.subjectID);
                            }
                }
        }
        private void DisableBoarding(bool enable = true)
        {
            if (enable)
            {
                GameSettings.EVA_Board.switchState = InputBindingModes.None;
                GameSettings.EVA_Board.switchStateSecondary = InputBindingModes.None;
            }
            else
            {
                GameSettings.EVA_Board.switchState = InputBindingModes.Any;
                GameSettings.EVA_Board.switchStateSecondary = InputBindingModes.Any;
            }
        }
        private bool CrewCanBoard(KerbalEVA kerbal)
        {
            bool partBoardable = false;
            //Done with checks now do transfer
            ScreenMessages sm = (ScreenMessages)GameObject.FindObjectOfType(typeof(ScreenMessages));
            for (int msgIndex = 0; msgIndex < sm.ActiveMessages.Count(); msgIndex++)
            {
                if (sm.ActiveMessages[msgIndex].message == "[B]: Board") { partBoardable = true; }
            }
            return partBoardable;
        }
        private Part GetKerbalTargetHatch(KerbalEVA kerbal)
        {
            return GetObjectField<Part>(typeof(KerbalEVA), kerbal, "currentAirlockPart");
        }
        private T GetObjectField<T>(Type type, object instance, string fieldName)
        {
            try
            {
                BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.NonPublic;
                FieldInfo field = type.GetField(fieldName, bindFlags);
                return (T)field.GetValue(instance);
            }
            catch
            {
                return default(T);
            }
        }
        private void updateBoardingStatus(int StatusChange)
        {
            // 0 = OK to Board Part
            // 2 = EVAKerbal has at least 1 Surface Sample and trying to board a command pod
            switch (StatusChange)
            {
                case 2:
                    BoardingMsgHeadingtxt = "Cannot store Surface Samples";
                    BoardingMsgLabeltxt = "There is no space to store the Surface Samples in the ";
                    BoardingMsgButtontxt = "Board Anyway\n(Dump Excess Surface Samples)";
                    break;
                default:
                    break;
            }
            BoardingMsgLabeltxt += part.partInfo.title + ".\n\nAre you sure you want to continue?";
        }
        #endregion


        #region PopUpGUI functions
        void OnGUI()
        {
            if (BoardPopupShow)
            {
                PopupWin = GUI.Window(673434644, PopupWin, OnPopupWindow, BoardingMsgHeadingtxt, PopupWinStyle);
            }
        }
        private void InitWindowStyle()
        {
            PopupWinStyle = new GUIStyle(HighLogic.Skin.window);

            PopupLabelStyle = new GUIStyle(HighLogic.Skin.label);
            PopupLabelStyle.fontSize = (HighLogic.Skin.font.fontSize - 4);
            PopupLabelStyle.fontStyle = FontStyle.Normal;

            PopupBtnStyle = new GUIStyle(HighLogic.Skin.button);
            //PopupBtnStyle.font = calibri;
            PopupBtnStyle.fontSize = (HighLogic.Skin.font.fontSize - 5);
            PopupBtnStyle.fontStyle = FontStyle.Bold;
            PopupBtnStyle.normal.textColor = Color.white;
            PopupBtnStyle.stretchWidth = true;
            PopupBtnStyle.fixedHeight = 30f;

            hasInitStyles = true;
        }
        private void InitWindowPoition()
        {
            PopupWinPosHeight = Screen.height / 2;
            PopupWinPosWidth = Screen.width / 2;
            PopupWin = new Rect(PopupWinPosWidth - 200, PopupWinPosHeight, 300, 160);
        }
        public void OnPopupWindow(int WindowID)
        {
            GUI.skin.label.alignment = TextAnchor.MiddleRight;
            GUILayout.Label(BoardingMsgLabeltxt, PopupLabelStyle);
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;

            


            if (GUILayout.Button(BoardingMsgButtontxt, PopupBtnStyle))
            {
                onConfirmDumpandBoard();
                isBoardingThisPart = true;
                BoardPopupShow = false;
            }
            if (GUILayout.Button("Cancel", PopupBtnStyle))
            {
                isBoardingThisPart = false;
                PartBoardingHatch = null;
                BoardPopupShow = false;
            }
            GUI.DragWindow();
        }
        #endregion


        public void HSStoreScienceData()
        {
            if (HighLogic.LoadedSceneIsFlight)
                if (FlightGlobals.ActiveVessel.vesselType == VesselType.EVA)
                {
                    
                    foreach (var EVADataContainer in FlightGlobals.ActiveVessel.FindPartModulesImplementing<IScienceDataContainer>())
                        foreach (ScienceData StoredSciData in EVADataContainer.GetData())
                        {
                            bool founddupe = false;
                            foreach (ScienceData onBoardSD in StockScienceContainer.GetData())
                            {
                                if (onBoardSD.subjectID == StoredSciData.subjectID)
                                    founddupe = true;
                            }

                            if (founddupe) 
                                ScreenMessages.PostScreenMessage(orangeTxt + "[" + part.partInfo.title + "]: <i>" + StoredSciData.subjectID + ":</i> already stored in this module.</color>", ScreenMsgDelay, ScreenMessageStyle.UPPER_LEFT);
                            else if (!Utilities.isSurfaceSample(StoredSciData))
                                {
                                    if(StockScienceContainer.AddData(StoredSciData))
                                        EVADataContainer.DumpData(StoredSciData);
                                }
                        }
                }
        }

        private void Update_StoreDataX()
        {
            if (part == null) return;
            StockScienceContainer = part.FindModuleImplementing<ModuleScienceContainer>();
            StockScienceContainer.Events["StoreDataExternalEvent"].guiActiveUnfocused = false;
            int StockEVAExperimentCount = (int)int.Parse(Regex.Match(StockScienceContainer.Events["StoreDataExternalEvent"].guiName, @"\(([^)]*)\)").Groups[1].Value);

            //Remove any SurfaceSample data from Store count
            if (EVAKerbalhasSurfaceSamples(out int sscount))
            {
                StockEVAExperimentCount -= sscount;
            }


            if (StockEVAExperimentCount != 0)
            {
                Events["StoreHSDataEvent"].active = true;
                Events["StoreHSDataEvent"].guiActive = true;
                Events["StoreHSDataEvent"].guiName = "Store Experiments.. (" + StockEVAExperimentCount + ")";
            }
            else
            {
                Events["StoreHSDataEvent"].guiName = "Store Experiments.. (0)";
                Events["StoreHSDataEvent"].guiActive = false;
                Events["StoreHSDataEvent"].active = false;
            }
        }


    }
}
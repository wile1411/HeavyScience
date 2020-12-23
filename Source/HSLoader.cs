/*
 * 
 * CyberKerb
 * Bootstrapped from https://github.com/zitron-git/KSPSerialIO
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.IO;
using System.Reflection;
using System.Diagnostics;

namespace HeavyScience
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class Configuration : MonoBehaviour
    {
        
        /// <summary>
        /// Version of the DLL.
        /// </summary>
        static public string HeavyScienceVersion;

        //masses used (in Kg
        public static float SurfaceSampleMass = 0.15f; // Default mass is 150kg, which is 0.15 in game mass in tons

        

        /// <summary>
        /// Awake() - Load components used by the mod at MainMenu.
        /// </summary>
        void Awake()
        {
            HeavyScienceVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            Log.Normal(String.Format(" Heavy Science version {0}", HeavyScienceVersion));


            if (!GameDatabase.Instance.IsReady())
            {
                Log.Warning("GameDatabase.IsReady is false");
                throw new Exception("HSLoader: GameDatabase is not ready.  Unable to continue.");
            }
            LoadMod_Defaults();
            scatterBuilder.InitialiseCBScatterData();

            scatterBuilder.getResourcesMesh();
            scatterBuilder.getResourcesMaterials();
            scatterBuilder.getResourcesTextures();

            SetBiomeDefaultScatter();
            SetBiomeSpecificScatter();
            SetScatterCustomisations();

            MakeCustomScatter();

        }

        void LoadMod_Defaults() 
        {
            //Load settings for HeavyScience settings
            foreach (UrlDir.UrlConfig node in GameDatabase.Instance.GetConfigs("HeavyScienceDefaults"))
            {
                if (node.config.HasValue("SurfaceSampleMass"))
                {
                    if (float.TryParse(node.config.GetValue("SurfaceSampleMass"), out SurfaceSampleMass))
                        Log.UserInfo("Surface Sample Mass set to:" + SurfaceSampleMass + " from settings cfg file");
                    else
                        Log.UserInfo("Surface Sample Mass defaulted to:" + SurfaceSampleMass);
                }

            }
        }

        void SetBiomeDefaultScatter() 
        {
            //load Body specific deafults to the biomeScatter class
            foreach (UrlDir.UrlConfig node in GameDatabase.Instance.GetConfigs("BodyScatterDefault"))
            {
                foreach (var biomeScatterItem in scatterBuilder.biomeScatterLib.Where(i => i.Value.bodyname.Equals(node.config.GetValue("name"))))
                {
                    biomeScatterItem.Value.bodyScatterID_Default = scatterBuilder.scatterLib[node.config.GetValue("bodyScatter")]; 
                    //Log.UserInfo(biomeScatterItem.Key + " now using: " + scatterBuilder.biomeScatterLib[biomeScatterItem.Key].bodyScatterID_Default.bodyScatterID);
                }
            }
        }

        void SetBiomeSpecificScatter()
        {
            //load Biome specific deafults to the biomeScatter class
            foreach (UrlDir.UrlConfig node in GameDatabase.Instance.GetConfigs("BodyScatterBiome"))
            {
                foreach (var biomeScatterItem in scatterBuilder.biomeScatterLib.Where(i => i.Value.bodyname.Equals(node.config.GetValue("name"))))
                {
                    if (node.config.HasValue("bodyScatterAlt"))
                    {
                        float AltPerc = float.Parse(node.config.GetValue("bodyScatterAltPerc"));
                        if (AltPerc != 1)
                        {
                            biomeScatterItem.Value.bodyScatterID_Alt = scatterBuilder.scatterLib[node.config.GetValue("bodyScatterAlt")]; 
                            biomeScatterItem.Value.AlternateOdds = AltPerc;
                            //Log.UserInfo("biomeScatterLib adding: " + biomeScatterItem.Value.bodyBiome + " scatter with " + node.config.GetValue("bodyScatterAlt"));
                        }
                        else
                        {
                            biomeScatterItem.Value.bodyScatterID_Default = scatterBuilder.scatterLib[node.config.GetValue("bodyScatterAlt")];  
                            //Log.UserInfo("biomeScatterLib overriding default: " + biomeScatterItem.Value.bodyBiome + " scatter with " + node.config.GetValue("bodyScatterAlt"));
                        }
                    }
                    if (node.config.HasValue("forceWater")) 
                    { 
                        biomeScatterItem.Value.forceWater = bool.Parse(node.config.GetValue("forceWater"));
                    }
                }
            }
        }


        void SetScatterCustomisations()
        {
            //Modify Scatter parameters to account for different size/shape
            foreach (UrlDir.UrlConfig node in GameDatabase.Instance.GetConfigs("customiseSurfaceSample"))
            {
                if (node.config.HasValue("scatterName")) 
                {
                    //foreach (scatterLibrary ScatterItem in scatterBuilder.scatterLib.FindAll(x => x.bodyScatterID.Equals(node.config.GetValue("scatterName"))))
                    foreach (var ScatterItem in scatterBuilder.scatterLib.Where(i => i.Key.Equals(node.config.GetValue("scatterName"))))
                    {
                        if (node.config.HasValue("meshScale"))
                        {
                            ScatterItem.Value.meshScale = float.Parse(node.config.GetValue("meshScale"));
                            ScatterItem.Value.bounceUpLimit = float.Parse(node.config.GetValue("bounceUpLimit"));
                            ScatterItem.Value.bounceDownLimit = float.Parse(node.config.GetValue("bounceDownLimit"));
                            //Log.UserInfo("scatterLib modifying: " + node.config.GetValue("scatterName") + " with MeshScale: " + ScatterItem.meshScale + " Up:" + ScatterItem.bounceUpLimit + " Down:" + ScatterItem.bounceDownLimit);
                            if (node.config.HasValue("bodyScatterID"))
                                ScatterItem.Value.bodyScatterID = node.config.GetValue("bodyScatterID");
                            if (node.config.HasValue("colorise"))
                                ScatterItem.Value.colorise = bool.Parse(node.config.GetValue("colorise"));
                        }
                    }
                }
            }
        }

        void MakeCustomScatter() 
        {
            //Load custom scatter texture definitions
            foreach (UrlDir.UrlConfig node in GameDatabase.Instance.GetConfigs("makeSurfaceSample"))
            {

                //Log.UserInfo("makescatter: " + node.config.GetValue("bodyScatterID"));
                string copyScatterFrom = node.config.GetValue("copyfrom");
                if (scatterBuilder.scatterLib.ContainsKey(copyScatterFrom))
                {
                    scatterLibrary copyScatterItem = scatterBuilder.scatterLib[copyScatterFrom];

                    scatterLibrary newScatterItem = new scatterLibrary();
                    newScatterItem.bodyScatterID = node.config.GetValue("bodyScatterID");
                    newScatterItem.mesh = copyScatterItem.mesh;
                    newScatterItem.meshScale = copyScatterItem.meshScale;
                    newScatterItem.materialScale = copyScatterItem.materialScale;
                    newScatterItem.bounceUpLimit = copyScatterItem.bounceUpLimit;
                    newScatterItem.bounceDownLimit = copyScatterItem.bounceDownLimit;
                    newScatterItem.isGenerated = true;
                    if (node.config.HasValue("copyMaterial"))
                    {
                        newScatterItem.material = GameObject.Instantiate(scatterBuilder.builtinMaterialLib[node.config.GetValue("copyMaterial")]);
                        if (node.config.HasValue("copyMaterialScale"))
                        {
                            newScatterItem.materialScale = float.Parse(node.config.GetValue("copyMaterialScale"));
                        }
                    }
                    else
                    {
                        newScatterItem.material = GameObject.Instantiate(copyScatterItem.material);
                    }
                    if (node.config.HasValue("Maintex"))
                    {
                        if (scatterBuilder.builtinTextureLib.ContainsKey(node.config.GetValue("Maintex")))
                        { 
                            Texture temptex = scatterBuilder.builtinTextureLib[node.config.GetValue("Maintex")];
                            newScatterItem.material.SetTexture("_MainTex", temptex);
                        }
                        else
                            Log.Error(node.config.GetValue("Maintex") + " IS NULL");
                        if (node.config.HasValue("BumpMap"))
                        {
                            if (scatterBuilder.builtinTextureLib.ContainsKey(node.config.GetValue("BumpMap"))) 
                                newScatterItem.material.SetTexture("_BumpMap", scatterBuilder.builtinTextureLib[node.config.GetValue("BumpMap")]);
                        }
                    }
                    scatterBuilder.scatterLib.Add(newScatterItem.bodyScatterID, newScatterItem);
                    //Log.UserInfo("scatterLib adding: " + newScatterItem.bodyScatterID + "         Shader:" + newScatterItem.material.shader.name + "  Cnt:" + scatterBuilder.scatterLib.Count);
                    if (node.config.HasValue("ReplaceBiome"))
                    { 
                        foreach (string replacebiomenode in node.config.GetValues("ReplaceBiome"))
                        {
                            if (scatterBuilder.biomeScatterLib.ContainsKey(replacebiomenode))
                            {
                                string oldscatterID = scatterBuilder.biomeScatterLib[replacebiomenode].bodyScatterID_Default.bodyScatterID;
                                scatterBuilder.biomeScatterLib[replacebiomenode].bodyScatterID_Default = newScatterItem;
                                scatterBuilder.biomeScatterLib[replacebiomenode].AlternateOdds = 0;
                                //Log.UserInfo("     replaced " + scatterBuilder.biomeScatterLib[replacebiomenode].bodyBiome + " was: " + oldscatterID + "  now: " + scatterBuilder.biomeScatterLib[replacebiomenode].bodyScatterID_Default.bodyScatterID);
                            }
                            else
                                Log.Error(replacebiomenode + " IS NULL");
                        }
                    }
                }
                else
                    Log.Error(copyScatterFrom +  " IS NULL");


            }
        }
    }
}

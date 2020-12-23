using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Reflection;
using UnityEditor;


namespace HeavyScience
{

    class scatterLibrary 
    {
        public string bodyScatterID = "";                // LandControl.scatters
        public string bodyname = "";                    // From FlightGlobals

        public GameObject scatterObj;
        public Mesh mesh;                               //  From LandControl
        public Material material;                       //  From LandControl

        public float meshScale = 0.15f;                  //  Set from ConfigNode
        public float materialScale = 1.0f;              //  Set from ConfigNode

        public float bounceUpLimit = 0.15f;              //  Set from ConfigNode
        public float bounceDownLimit = -0.25f;                //  Set from ConfigNode

        public bool isGenerated = false;                    
        public bool colorise = true;

        public bool isCollectable = true;
    }

    class biomeScatter
    {
        public string bodyname = "";                    // From FlightGlobals
        public string bodyBiome = "";                   // From CBAttributeMapSO.MapAttribute
        public bool isMini = false;
        public Color biomeColor = Color.clear;          // From CBAttributeMapSO.MapAttribute
        public scatterLibrary bodyScatterID_Default;              // Set from Config Nodes - BodyScatterDefault
        public scatterLibrary bodyScatterID_Alt;            // Set from Config Nodes - BodyScatterBiome
        public float AlternateOdds = 0f;                // Set from Config Nodes - BodyScatterBiome

        public bool forceWater = false;                 // Set from Config Nodes - BodyScatterBiome
    }
    class scatterBuilder
    {
        public static Dictionary<string, biomeScatter> biomeScatterLib = new Dictionary<string, biomeScatter>();
        public static Dictionary<string, scatterLibrary> scatterLib = new Dictionary<string, scatterLibrary>();

        public static Dictionary<string, Texture> builtinTextureLib = new Dictionary<string, Texture>();
        public static Dictionary<string, Material> builtinMaterialLib = new Dictionary<string, Material>();
        public static bool BreakingGroundInstalled = false;

        public static Mesh iceCreamConeMesh;
        //public static GameObject coneObj;


        public static GameObject getWaterSphereScatter(biomeScatter biomeitem)
        {
            Color bodyBiomecolor = Color.white;
            if (biomeitem == null)
            {
                bodyBiomecolor = new Color(0.678f, 0.847f, 0.902f, 0.7f);
            }
            else
            {
                bodyBiomecolor = biomeitem.biomeColor;
                Color alphacolor = bodyBiomecolor;
                alphacolor.a = 0.8f;
                bodyBiomecolor = alphacolor;
            }

            GameObject waterSphere = new GameObject("Watersphere");
            MeshFilter meshfilter = waterSphere.AddComponent<MeshFilter>();
            MeshRenderer meshrenderer = waterSphere.AddComponent<MeshRenderer>();

            GameObject primSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            meshfilter.mesh = GameObject.Instantiate(primSphere.GetComponent<MeshFilter>().mesh);
            GameObject.Destroy(primSphere);

            meshrenderer.material = GameObject.Instantiate(builtinMaterialLib["PoolWater_0"]); 
            meshrenderer.material.SetColor("_Color", bodyBiomecolor);
            

            return waterSphere;
        }

        public static GameObject getRockScatter(scatterLibrary scatteritem, biomeScatter biomeitem, bool extraModel)
        {
            GameObject rockModel = new GameObject("surfaceSample_Rock");
            MeshFilter rockFilterer;
            MeshRenderer rockRenderer;
            if (scatteritem.scatterObj != null)
            {
                rockModel = GameObject.Instantiate(scatteritem.scatterObj);
                //rockFilterer = rockModel.GetComponent<MeshFilter>();
                rockRenderer = rockModel.GetComponent<MeshRenderer>();
                //Log.UserInfo("-----OBJ creation: " + scatteritem.scatterObj.name);
            }
            else
            {
                rockFilterer = rockModel.AddComponent<MeshFilter>();
                rockFilterer.mesh = GameObject.Instantiate(scatteritem.mesh);

                rockRenderer = rockModel.AddComponent<MeshRenderer>();
                rockRenderer.material = GameObject.Instantiate(scatteritem.material);
                //Log.UserInfo("-----Manual OBJ creation: " + scatteritem.bodyScatterID);
            }

            if (biomeitem != null && scatteritem.colorise)
            {
                Color alphacolor;
                alphacolor = biomeitem.biomeColor;
                alphacolor.a = 0.4f;
                rockRenderer.material.SetColor("_Color", alphacolor);
                //Log.UserInfo(biomeitem.bodyBiome +"  Color: " + alphacolor + "  ScatterID: " + scatteritem.bodyScatterID);
            }
            //else
                //Log.UserInfo(scatteritem.bodyScatterID + " has NO biomeitem");


            //Add chance for cone if Minmus Flats
            if (extraModel)
            {
                GameObject newcone = scatterBuilder.addIceCreamCone();
                newcone.transform.SetParent(rockModel.transform, true);
            }

            return rockModel;
        }
        
        public static GameObject getCubeScatter(scatterLibrary scatteritem, biomeScatter biomeitem)
        {

            

            GameObject comboObj = new GameObject("surfaceSample_Joint");

            GameObject blockCube = new GameObject("surfaceSample_Slab");
            MeshFilter blockmeshfilter = blockCube.AddComponent<MeshFilter>();
            MeshRenderer blockmeshrenderer = blockCube.AddComponent<MeshRenderer>();

            GameObject primCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blockmeshfilter.mesh = GameObject.Instantiate(primCube.GetComponent<MeshFilter>().mesh);
            GameObject.Destroy(primCube);


            

            blockmeshrenderer.material = GameObject.Instantiate(scatteritem.material);

            blockCube.transform.localRotation = blockCube.transform.localRotation * Quaternion.Euler(-4, 90, -3);
            blockCube.transform.localPosition = new Vector3(0f, 0.53f, 0f);
            if (blockmeshrenderer.material.name.Contains("Goo") || blockmeshrenderer.material.name.Contains("Wires"))
                blockCube.transform.localScale = new Vector3(2.5f, 0.05f, 2.5f);
            else
                blockCube.transform.localScale = new Vector3(2.5f, 0.4f, 2.5f);

            blockmeshrenderer.material.mainTextureScale = new Vector2(scatteritem.materialScale, scatteritem.materialScale);


            GameObject rockModel = new GameObject("surfaceSample_Rock");
            MeshFilter rockFilterer = rockModel.AddComponent<MeshFilter>();
            MeshRenderer rockRenderer = rockModel.AddComponent<MeshRenderer>();
            rockFilterer.mesh = GameObject.Instantiate(scatteritem.mesh);
            rockRenderer.material = GameObject.Instantiate(builtinMaterialLib["ksc_exterior_terrain_ground_0"]);
            rockModel.transform.localScale = new Vector3(1.1f, 0.4f, 1.1f);

            //Log.UserInfo("-----Manual OBJ creation: " + scatteritem.bodyScatterID + " with Shader:" + rockRenderer.material.shader + " mixed with: " + rockRenderer.material.shader);

            rockModel.transform.SetParent(comboObj.transform, true);
            blockCube.transform.SetParent(comboObj.transform, true);
            

            return comboObj;
        }

        internal static GameObject addIceCreamCone()
        {
            GameObject icecreamcone = new GameObject("IceCreamCone");
            MeshFilter meshfilter = icecreamcone.AddComponent<MeshFilter>();
            MeshRenderer meshrenderer = icecreamcone.AddComponent<MeshRenderer>();

            //coneObj = GameDatabase.Instance.databaseModel.Find(x => x.name.Contains("wafflecone"));
            meshfilter.mesh = GameObject.Instantiate(iceCreamConeMesh); 
            
            meshrenderer.material = new Material(Shader.Find("KSP/Bumped Specular"));
            meshrenderer.material.SetTexture("_MainTex", GameDatabase.Instance.GetTexture("HeavyScience/Assets/waffle", false));
            meshrenderer.material.SetTexture("_BumpMap", GameDatabase.Instance.GetTexture("HeavyScience/Assets/waffle_n", true));
            icecreamcone.transform.localScale = new Vector3(14f, 18f, 14f);
            icecreamcone.transform.localRotation = Quaternion.Euler(65,-180,0);
            icecreamcone.transform.localPosition = new Vector3(0f, -0.48f, 1.7f);

            return icecreamcone;
        }



        internal static void getResourcesMesh() 
        {
            MeshFilter[] foundMeshFilters = Resources.FindObjectsOfTypeAll<MeshFilter>();
            for (int i = foundMeshFilters.Length; i-- > 0;) 
            {
                if (foundMeshFilters[i].name.Contains("waffle")) 
                {
                    iceCreamConeMesh = foundMeshFilters[i].mesh;
                    //Log.UserInfo("Searching Resources/MeshFilters & found: " + foundMeshFilters[i].name);
                    return;
                }
            }
        }
        internal static void getResourcesMaterials()
        {
            Material[] foundMaterials = Resources.FindObjectsOfTypeAll<Material>();
            int counter = 0;
            for (int i = foundMaterials.Length; i-- > 0;)
            {
                if (!foundMaterials[i].name.Contains("(Instance)"))
                {
                    counter = 0;
                    while (builtinMaterialLib.ContainsKey(foundMaterials[i].name + "_" + counter))
                    {
                        counter++;
                    }
                    builtinMaterialLib.Add(foundMaterials[i].name + "_" + counter, foundMaterials[i]);
                }
            }
        }

        internal static void getResourcesTextures()
        {
            Texture[] foundTextures = Resources.FindObjectsOfTypeAll<Texture>();
            //Log.UserInfo("Looking through Resources Textures: " + foundTextures.Length);
            int counter = 0;
            foreach (Texture texture in foundTextures)
            {
                if (!texture.name.Contains("/") || texture.name.Contains("lines"))
                { 
                    counter = 0;
                    while (builtinTextureLib.ContainsKey(texture.name + "_" + counter))
                    {
                        counter++;
                    }
                    builtinTextureLib.Add(texture.name + "_" + counter, texture);
                    //Log.UserInfo("Resources Texture found: " + texture.name + "_" + counter);
                }
            }
        }






        internal static void InitialiseCBScatterData()
        {
            //Get a list of CBs
            if (FlightGlobals.fetch != null)
                foreach (CelestialBody cb in FlightGlobals.Bodies) 
                {
                    //Debug.Log("Looking at: " + cb.name);
                    //Adding all Biomes from this CelestialBody into a reference list
                    if (cb.BiomeMap != null)
                    { 
                        foreach (CBAttributeMapSO.MapAttribute biomeAttrib in cb.BiomeMap.Attributes)
                        { 
                            if(biomeAttrib != null)
                            {
                                biomeScatter inputbiome = new biomeScatter();
                                inputbiome.bodyname = cb.name;
                                inputbiome.bodyBiome = cb.name + Regex.Replace(biomeAttrib.name, " ", "");
                                inputbiome.biomeColor = biomeAttrib.mapColor;
                                biomeScatterLib.Add(inputbiome.bodyBiome, inputbiome);
                                //Log.UserInfo("biomeScatterLib Added: " + inputbiome.bodyBiome + " : " + inputbiome.biomeColor);
                            }
                        }

                        //Scaping the MiniBiomes from Kerbin so all biomes have a scatter
                        foreach (MiniBiome minibiome in cb.MiniBiomes)
                        {
                            biomeScatter inputbiome = new biomeScatter();
                            inputbiome.bodyname = cb.name;
                            inputbiome.bodyBiome = cb.name + Regex.Replace(minibiome.GetDisplayName, " ", "");
                            inputbiome.isMini = true;

                            if (!biomeScatterLib.ContainsKey(inputbiome.bodyBiome)) 
                            { 
                                biomeScatterLib.Add(inputbiome.bodyBiome, inputbiome);
                                //Log.UserInfo("biomeScatterLib Added MINI: " + inputbiome.bodyBiome + " : " + minibiome.name);
                            }
                            //else
                              //Log.UserInfo("biomeScatterLib skipped MINI: " + inputbiome.bodyBiome + " : " + minibiome.name + " : " + minibiome.GetType());
                        }
                    }
                    else
                    {
                        //Log.UserInfo(cb.name + ": No Biomes for this Body");
                    }


                    if (cb.pqsController != null)
                    {
                        //Scraping all the default scatter deatils into a reference list
                        PQSLandControl LandControl = cb.pqsController.GetComponentInChildren<PQSLandControl>(true);
                        if (LandControl != null)
                        {
                            foreach (var scatteritems in LandControl.scatters)
                            {
                                scatterLibrary newScatterItem = new scatterLibrary();
                                newScatterItem.bodyScatterID = cb.name + "_" + scatteritems.scatterName;
                                newScatterItem.bodyname = cb.name;
                                if (scatteritems.baseMesh != null)
                                    newScatterItem.mesh = scatteritems.baseMesh;
                                if (scatteritems.material != null)
                                    newScatterItem.material = scatteritems.material; 
                                scatterLib.Add(newScatterItem.bodyScatterID, newScatterItem);
                                //Log.UserInfo("scatterLib Adding: " + newScatterItem.bodyScatterID + "         Shader:" + scatteritems.material.shader.name + "  Cnt:" + scatterLib.Count);
                                //if (scatteritems.baseMesh != null)
                                //    Log.UserInfo("Mesh: " + scatteritems.baseMesh.name);
                            }
                        }
                        

                        //Scraping all the ROC collectables into a reference list - Requires Breaking Ground DLC
                        //Thought this was best way of implementing the mod without having to check that the DLC is installed
                        PQSROCControl rockController = cb.pqsController.GetComponentInChildren<PQSROCControl>(true);
                        if (rockController != null)
                        {
                            BreakingGroundInstalled = true;
                            List<LandClassROC> landClasses = rockController.rocs;
                            foreach (LandClassROC lcR in landClasses)
                            {
                                GameObject rocObject = (GameObject)lcR.GetType().GetField("rocObject", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(lcR);
                                ROC rocRef = rocObject.GetComponent<ROC>();
                                if (rocRef.canbetaken)
                                {
                                    scatterLibrary newScatterItem = new scatterLibrary();
                                    MeshRenderer ROCRenderer = rocObject.GetComponentInChildren<MeshRenderer>();
                                    MeshFilter ROCFilter = rocObject.GetComponentInChildren<MeshFilter>();
                                    
                                    newScatterItem.bodyScatterID = "ROCScience_" + rocRef.type;
                                    newScatterItem.bodyname = "ROCScience_" + rocRef.type;
                                    newScatterItem.mesh = ROCFilter.mesh;
                                    newScatterItem.material = ROCRenderer.material;
                                    newScatterItem.isCollectable = rocRef.canbetaken;
                                    scatterLib.Add(newScatterItem.bodyScatterID, newScatterItem);
                                    //Log.UserInfo("scatterLib adding: " + newScatterItem.bodyScatterID + "         Shader:" + ROCRenderer.material.shader.name + "  Cnt:" + scatterLib.Count + "  Mesh:" + ROCFilter.mesh.name);
                                }
                            }
                        }
                        


                        //Scrape one more item to make the visit memorable
                        //Also add it into the bopScatterLibdefinition.
                        if (cb.name == "Bop")
                        {
                            PQSCity city = cb.pqsController.GetComponentInChildren<PQSCity>(true);
                            if (city != null)
                            {
                                Transform foundtrans = city.transform.GetChild(0).Find("Dead Kraken's Eye");
                                if (foundtrans != null)
                                {
                                    scatterLibrary newScatterItem = new scatterLibrary();
                                    newScatterItem.bodyScatterID = "Bop_Stone";
                                    newScatterItem.bodyname = "Bop_Stone";
                                    newScatterItem.scatterObj = foundtrans.gameObject;
                                    scatterLib.Add(newScatterItem.bodyScatterID, newScatterItem);

                                    biomeScatterLib["BopPoles"].bodyScatterID_Alt = scatterLib["Bop_Stone"];
                                    biomeScatterLib["BopPoles"].AlternateOdds = 0f;
                                    //Log.UserInfo("scatterLib adding: " + newScatterItem.bodyScatterID + "  Cnt:" + scatterLib.Count + "  Obj:" + newScatterItem.scatterObj.name);

                                }
                            }
                        }
                    }
                }
            //else
            //    Log.UserInfo("Flight Globals Null");

        }

    }
}

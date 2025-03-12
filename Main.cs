using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GPUInstancer;
using MelonLoader;
using UnityEngine;
using Harmony;
using HarmonyLib;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace MapImporter
{
    public class Main : MelonMod
    {
        string path = "Mods/Maps/";
        string[] files;

        bool menuOpen = false;
        
        Terrain terrain;
        int mapIndex = 0;

        List<bool> mapSelections = new List<bool>(10);

        int heightY = 4096;

        // Returns every file in a specified path
        string[] getFiles(string path)
        {
            
            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path);
                return files;
            }
            return null;
            
        }

        void instantiateMapSelection(List<bool> mapSelections, int length)
        {
            for (int i = 0; i < length; i++)
            {
                mapSelections.Add(false);
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Garage")
            {
                GameObject mainMenu = GameObject.Find("MainMenu");

                menuOpen = true;

                files = getFiles(path);

                instantiateMapSelection(mapSelections, files.Length);
                

                return;
            }
            
            menuOpen = false;

            if (sceneName == "Idaho")
            {
                if (mapIndex <= 0)
                {
                    int lenght = mapSelections.Count;

                    for (int i = 0; i < lenght; i++) 
                    { 
                        if (mapSelections[i])
                        {
                            loadMap(i + 1);
                            return;
                        }
                    }
                    return;

                }

                loadMap(mapIndex);

                //setTrees(terrains);

                mapIndex = 0;

            }
        }

        void loadMap(int mapIndex)
        {
            string filepath = files[mapIndex - 1];
            Melon<Main>.Logger.Msg("Loaded: " + filepath);


            GameObject ramSpline = GameObject.Find("RamSpline");
            ramSpline.SetActive(false);

            GameObject border = GameObject.Find("Border");
            border.SetActive(false);

            Terrain[,] terrains = GetTerrains();

            if (terrains == null)
            {
                Melon<Main>.Logger.Msg("Could not get terrains");
                return;
            }

            if (filepath.EndsWith(".txt"))
            {
                float[,] map = ReadTxtFile(filepath);
                LoadTerrains(map, terrains);
            }
            else if (filepath.EndsWith(".raw"))
            {
                float[,] map = ReadRawHeightmap(filepath, 8192, true);
                LoadTerrains(map, terrains);
            }
            else
            {
                Melon<Main>.Logger.Msg("Wrong file format");
                return;
            }
        }

        void setTrees(Terrain[,] terrains)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    Terrain terrain = terrains[y, x];
                    int treeCount = terrain.terrainData.treeInstanceCount;
                    

                    
                    TreeInstance[] trees = new TreeInstance[1];
                    
                    trees[0].widthScale = 10;
                    trees[0].heightScale = 10;
                    Vector2 treePos = new Vector2(UnityEngine.Random.Range(0, 100) / 100, UnityEngine.Random.Range(0, 100) / 100);
                    trees[0].position = new Vector3(treePos.x, 0, treePos.y);
                    Melon<Main>.Logger.Msg("treeCount: " + treeCount + " treePos: " + trees[0].position + " TerrainTreePos: " + terrain.terrainData.treeInstances[0].position);



                    terrains[x, y].terrainData.treeInstances = trees;

                }
            }
        }

        // Returns a 2d array containing the terrains in the scene
        Terrain[,] GetTerrains()
        {
            Terrain[,] terrains = new Terrain[8, 8];
            for (int y = 0; y < 8; y++)
            {
                for(int x = 0; x < 8; x++)
                {
                    GameObject terrainRoot = GameObject.Find("Idaho_Alpha_"+ x + "_" + y);

                    if (terrainRoot == null)
                    {
                        return null;
                    }

                    terrains[x, y] = terrainRoot.GetComponent<Terrain>();
                    //Melon<Class1>.Logger.Msg("Terrain: " + terrainRoot.name + "  heightmapRes: " + terrains[x, y].terrainData.heightmapResolution + "  size: " + terrains[x, y].terrainData.size);

                }
            }
            return terrains;
        }

        
        // Reads a raw file as a heightmap
        float[,] ReadRawHeightmap(string filePath, int resolution, bool is16Bit)
        {
            if (!File.Exists(filePath))
            {
                Melon<Main>.Logger.Msg($"RAW file not found: {filePath}");
                return null;
            }

            byte[] rawData = File.ReadAllBytes(filePath);
            int expectedSize = resolution * resolution * (is16Bit ? 2 : 1);

            if (rawData.Length != expectedSize)
            {
                Melon<Main>.Logger.Msg($"Invalid RAW file size: Expected {expectedSize} bytes, got {rawData.Length} bytes.");
                return null;
            }

            float[,] heights = new float[resolution, resolution];
            int index = 0;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    if (is16Bit)
                    {
                        // Read 16-bit value (Little Endian)
                        ushort value = (ushort)(rawData[index] | (rawData[index + 1] << 8));
                        heights[y, x] = value / 65535f; // Normalize to 0-1 range
                        index += 2;
                    }
                    else
                    {
                        // Read 8-bit value
                        heights[y, x] = rawData[index] / 255f;
                        index++;
                    }
                }
            }
            

            Melon<Main>.Logger.Msg($"Successfully loaded heightmap from {filePath}");
            return heights;
        }

        // Iterates over every terrain and loads the corresponding part of the heightmap
        void LoadTerrains(float[,] heights, Terrain[,] terrains)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    Terrain currentTerrain = terrains[y, x];

                    currentTerrain.Flush();

                    int resolution = currentTerrain.terrainData.heightmapResolution;
                    Vector3 size = new Vector3(512, heightY, 512);

                    currentTerrain.terrainData.size = size;
                    currentTerrain.terrainData.heightmapResolution = 1025;

                    float[,] heightmap = DivideHeightMap(heights, resolution, x, y);
                    currentTerrain.terrainData.SetHeights(0, 0, heightmap);

                    currentTerrain.Flush();

                    List<TreeInstance> trees = new List<TreeInstance>(currentTerrain.terrainData.treeInstances);

                    foreach (TreeInstance tree in trees)
                    {
                        tree.position.Equals(Vector3.zero);
                    }

                    currentTerrain.terrainData.treeInstances = trees.ToArray();

                }
            }
        }

        float[,] DivideHeightMap(float[,] heights, int resolution, int x, int y)
        {
            int totalRes = (int)Mathf.Sqrt(heights.Length);
            float[,] heightmap = new float[resolution, resolution];

            for (int y2 = 0; y2 < resolution; y2++)
            {
                for (int x2 = 0; x2 < resolution; x2++)
                {
                    int globalX = x2 + x * resolution;
                    int globalY = y2 + y * resolution;

                    if (globalX < totalRes && globalY < totalRes)
                    {
                        // Copy valid heightmap values
                        heightmap[x2, y2] = heights[globalX, globalY];
                    }
                    else
                    {
                        // Set out-of-bounds areas to zero
                        heightmap[x2, y2] = 0f;
                    }
                }
            }

            return heightmap;
        }

        float[,] ReadTxtFile(string filePath) 
        {
            FileInfo fileInfo = new FileInfo(filePath);
            StreamReader sr = fileInfo.OpenText();

            string fl = sr.ReadLine();

            if (fl != "HeightMap")
            {
                Melon<Main>.Logger.Msg("file not correct type");
                return null;
            }

            int heightMapWidth = int.Parse(sr.ReadLine().Substring(16));
            int heightMapHeight = int.Parse(sr.ReadLine().Substring(17));

            int width = int.Parse(sr.ReadLine().Substring(14));
            int length = int.Parse(sr.ReadLine().Substring(14));
            int height = int.Parse(sr.ReadLine().Substring(14));

            float[,] heights = new float[heightMapWidth, heightMapHeight];

            for (int x = 0; x < heightMapWidth; x++)
            {
                for (int y = 0; y < heightMapHeight; y++)
                {
                    heights[x, y] = float.Parse(sr.ReadLine()) / 100000;
                }
            }

            return heights;
        }

        public override void OnInitializeMelon()
        {
            MelonEvents.OnGUI.Subscribe(DrawMenu, 100);
        }

        private Rect menuRect = new Rect(Screen.width - 310, 10, 300, 500); // Initial position and size

        private void DrawMap(string name, int index, Vector2Int size)
        {
            int height = 40;
            int width = size.x - 30;
            int offsetTop = 60;
            int offsetBetween = 10;

            // Adjust positions relative to the window instead of screen coordinates

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.margin.left = 20;


            Rect backgroundRect = new Rect(size.x / 2 - width / 2, offsetTop + (height + offsetBetween) * index, width, height);
            Rect labelRect = new Rect(size.x / 2 - width / 2, offsetTop + (height + offsetBetween) * index, width / 3 * 2, height);
            Rect buttonRect = new Rect(size.x / 2 + backgroundRect.width / 5 - 10, offsetTop + (height + offsetBetween) * index + height / 2 - 10, 80, 20);
            Rect toggleRect = new Rect(10f, offsetTop + (height + offsetBetween) * index, 10, 10);
            bool selection = mapSelections[index];

            mapSelections[index] = GUI.Toggle(toggleRect, selection, "");

            GUI.Box(backgroundRect, "");
            GUI.Label(labelRect, name, labelStyle);

            if (GUI.Button(buttonRect, "Load Map"))
            {
                mapIndex = index + 1;
                SceneManager.LoadScene(4);
            }
        }

        private void DrawMenuWindow(int windowID)
        {
            GUIStyle centeredStyle = new GUIStyle(GUI.skin.label);
            centeredStyle.alignment = TextAnchor.MiddleCenter;

            Vector2Int menuSize = new Vector2Int((int)menuRect.width, (int)menuRect.height);

            GUI.Label(new Rect(20, 30, 200, 20), "Terrain Height: ");
            heightY = int.Parse(GUI.TextField(new Rect(menuSize.x - 80, 30, 60, 20), heightY.ToString()));

            if (files != null)
            {
                int i = 0;
                foreach (string file in files)
                {
                    DrawMap(file, i, menuSize);
                    i++;
                }
            }

            GUI.Label(new Rect(menuSize.x / 2 - 100, menuSize.y - 40, 200, 20), "Made by Samisalami", centeredStyle);

            // Make the window draggable
            GUI.DragWindow(new Rect(0, 0, menuSize.x, 20));
        }

        private void DrawMenu()
        {
            if (menuOpen)
            {
                menuRect = GUI.Window(0, menuRect, DrawMenuWindow, "Terrain Importer");
            }
        }
    }
}

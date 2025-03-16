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
using System.ComponentModel;
using UnityEngine.Windows;

namespace MapImporter
{
    public class Main : MelonMod
    {
        string path = "Mods/Maps/";

        Dictionary<string, (string, string)> mapFiles;

        bool menuOpen = false;

        int mapIndex = 0;

        int heightY = 4096;

        int treeAmount = 0;

        bool generateTrees = false;

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

        // Returns a dictionary containing the map folders with a heightmap and treemask
        Dictionary<string, (string rawFile, string pngFile)> GetMapFiles(string path)
        {
            Dictionary<string, (string, string)> mapFolders = new Dictionary<string, (string, string)>();

            if (Directory.Exists(path))
            {
                string[] directories = Directory.GetDirectories(path); // Get all folders inside Maps/

                foreach (string dir in directories)
                {
                    string rawFile = Directory.GetFiles(dir, "*.raw").FirstOrDefault();
                    string pngFile = Directory.GetFiles(dir, "*.png").FirstOrDefault();

                    if (!string.IsNullOrEmpty(rawFile))
                    {
                        mapFolders[dir] = (rawFile, pngFile);
                    }
                }
            }
            else
            {
                Melon<Main>.Logger.Msg("Maps folder does not exist");
                return null;
            }

            return mapFolders;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Garage")
            {
                GameObject mainMenu = GameObject.Find("MainMenu");

                menuOpen = true;

                mapFiles = GetMapFiles(path);

                return;
            }

            menuOpen = false;

            if (sceneName == "Idaho")
            {
                if (mapIndex <= 0)
                {
                    return;
                }

                var mapList = mapFiles.ToList();

                loadMap(mapList[mapIndex - 1].Value.Item1, mapList[mapIndex - 1].Value.Item2);

                mapIndex = 0;

                changeMiniMap();
            }
        }

        // Test: changes the minimap (does not work)
        void changeMiniMap()
        {
            GameObject mapView = GameObject.Find("LevelEssentials/Map/MapView");

            MapViewController mapViewController = mapView.GetComponent<MapViewController>();

            if (mapViewController != null)
            {
                FieldInfo field = mapViewController.GetType().GetField("PCIODLFDCNE", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    Texture2D red = new Texture2D(512, 512);

                    for (int x = 0; x < 512; x++)
                    {
                        for (int y = 0; y < 512; y++)
                        {
                            red.SetPixel(x, y, new Color(255, 0, 0));
                        }
                    }

                    field.SetValue(mapView, red);
                }
                else
                {
                    Melon<Main>.Logger.Msg("Could not find field");
                }
            }
        }

        // Loads the desired map
        void loadMap(string rawFilePath, string treeMaskPath)
        {
            Melon<Main>.Logger.Msg("Loaded: " + rawFilePath);

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

            if (rawFilePath.EndsWith(".txt"))
            {
                float[,] map = ReadTxtFile(rawFilePath);
                LoadTerrains(map, terrains);
            }
            else if (rawFilePath.EndsWith(".raw"))
            {
                float[,] map = ReadRawHeightmap(rawFilePath, 8192, true);
                LoadTerrains(map, terrains);
            }
            else
            {
                Melon<Main>.Logger.Msg("Wrong file format");
                return;
            }

            
            if (File.Exists(treeMaskPath) && generateTrees == false)
            {
                bool[,] treeMask = readPng(treeMaskPath);
                if (treeMask != null)
                {
                    TreeImporter.setTreesFromMask(terrains, treeMask);
                    return; // If tree mask was used, skip random tree placement.
                }
            }

            // Fall back to random tree placement if treeAmount (from GUI) is greater than 0.
            if (treeAmount > 0)
            {
                TreeImporter.setRandomTrees(terrains, treeAmount);
            }
        }

        // Modified: Removed treeAmount++ to avoid interference with GUI value.
        bool[,] readPng(string filePath)
        {
            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData); // Auto-resizes the texture dimensions.
            }
            else
            {
                Melon<Main>.Logger.Msg("Could not find file");
                return null;
            }

            if (tex == null)
            {
                Melon<Main>.Logger.Msg("Texture not loaded correctly");
                return null;
            }

            bool[,] treeMask = new bool[tex.width, tex.height];

            // Loop through each pixel but flip the Y coordinate.
            // This will automatically invert the image vertically.
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    // Flip the vertical coordinate: use flippedY = tex.height - 1 - y.
                    int flippedY = tex.height - 1 - y;
                    Color pixel = tex.GetPixel(x, flippedY);
                    // If pixel is not black, mark it as true.
                    if (pixel.r != 0 && pixel.g != 0 && pixel.b != 0)
                    {
                        treeMask[x, y] = true;
                    }
                    else
                    {
                        treeMask[x, y] = false;
                    }
                }
            }

            return treeMask;
        }

        // Returns a 2d array containing the terrains in the scene
        Terrain[,] GetTerrains()
        {
            Terrain[,] terrains = new Terrain[8, 8];
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    GameObject terrainRoot = GameObject.Find("Idaho_Alpha_" + x + "_" + y);

                    if (terrainRoot == null)
                    {
                        return null;
                    }

                    terrains[x, y] = terrainRoot.GetComponent<Terrain>();

                    //Melon<Class1>.Logger.Msg("Terrain: " + terrainRoot.name + "  heightmapRes: " + terrains[x, y].terrainData.heightmapResolution + "  size: " + terrains[x, y].terrainData.size);

                }
            }
            Melon<Main>.Logger.Msg("treeWIdthScale: " + terrains[0, 0].terrainData.treeInstances[0].widthScale + " treeHeightScale: " + terrains[0, 0].terrainData.treeInstances[0].heightScale);
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
                }
            }
        }

        // Returns part of heightmap depending on the resolution and terrain position
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

        // Reads a text file and returns a heightmap
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

        // Draws the map container
        private void DrawMap(string name, int index, Vector2Int size)
        {
            int height = 40;
            int width = size.x - 40;
            int offsetTop = 0;
            int offsetBetween = 10;
            int offsetWidth = -15;

            // Adjust positions relative to the window instead of screen coordinates

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.margin.left = 20;


            Rect backgroundRect = new Rect(size.x / 2 - width / 2 + offsetWidth, offsetTop + (height + offsetBetween) * index, width, height);
            Rect labelRect = new Rect(size.x / 2 - width / 2 + offsetWidth, offsetTop + (height + offsetBetween) * index, width / 3 * 2, height);
            Rect buttonRect = new Rect(size.x / 2 + backgroundRect.width / 5 - 10 + offsetWidth, offsetTop + (height + offsetBetween) * index + height / 2 - 10, 80, 20);
            Rect toggleRect = new Rect(10f + offsetWidth, offsetTop + (height + offsetBetween) * index, 10, 10);

            GUI.Box(backgroundRect, "");
            GUI.Label(labelRect, name, labelStyle);

            if (GUI.Button(buttonRect, "Load Map"))
            {
                mapIndex = index + 1;
                SceneManager.LoadScene(4);
            }
        }

        private Vector2 scrollPosition = Vector2.zero; // Scroll position

        // Draws the menu window
        private void DrawMenuWindow(int windowID)
        {
            GUIStyle centeredStyle = new GUIStyle(GUI.skin.label);
            centeredStyle.alignment = TextAnchor.MiddleCenter;

            Vector2Int menuSize = new Vector2Int((int)menuRect.width, (int)menuRect.height);

            GUI.Label(new Rect(20, 30, 200, 20), "Terrain Height: ");
            heightY = int.Parse(GUI.TextField(new Rect(menuSize.x - 80, 30, 60, 20), heightY.ToString()));
            GUI.Label(new Rect(20, 50, 200, 20), "Tree Amount: ");
            treeAmount = int.Parse(GUI.TextField(new Rect(menuSize.x - 80, 50, 60, 20), treeAmount.ToString()));
            generateTrees = GUI.Toggle(new Rect(menuSize.x - 100, 50, 20, 20), generateTrees, "");

            int contentHeight = (mapFiles != null) ? (mapFiles.Count * 50 + 20) : 0; // Dynamic height for scrolling

            scrollPosition = GUI.BeginScrollView(
                new Rect(10, 80, menuSize.x - 15, menuSize.y - 120),  // Scroll view area
                scrollPosition,
                new Rect(0, 0, menuSize.x - 35, contentHeight),       // Content area size
                false,                                                // Horizontal scrolling disabled
                true                                                  // Vertical scrolling enabled
            );

            if (mapFiles != null)
            {
                int i = 0;
                foreach (var kvp in mapFiles)
                {
                    DrawMap(kvp.Key, i, menuSize);
                    i++;
                }
            }

            GUI.EndScrollView();

            GUI.Label(new Rect(menuSize.x / 2 - 100, menuSize.y - 40, 200, 20), "Made by Samisalami", centeredStyle);

            // Make the window draggable
            GUI.DragWindow(new Rect(0, 0, menuSize.x, 20));
        }

        // Draws the menu
        private void DrawMenu()
        {
            if (menuOpen)
            {
                menuRect = GUI.Window(0, menuRect, DrawMenuWindow, "Terrain Importer");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;

namespace MapImporter
{
    internal class Map
    {
        private MapData mapData;
        private Main main;
        

        public Map(Main main, MapData mapData)
        {
            this.main = main;
            this.mapData = mapData;
        }

        // Returns a 2d array containing the terrains in the scene
        private Terrain[,] GetTerrains()
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
            return terrains;
        }

        // Loads the map from the given file path and applies it to the terrains
        public void loadMap(string rawFilePath, string treeMaskPath)
        {
            Melon<Main>.Logger.Msg("Loaded: " + rawFilePath);

            GameObject ramSpline = GameObject.Find("RamSpline");
            ramSpline.SetActive(false);

            GameObject border = GameObject.Find("Border");
            border.SetActive(false);

            Terrain[,] terrains = GetTerrains();

            if (terrains == null)
            {
                Melon<Main>.Logger.Error("Could not get terrains");
                return;
            }

            if (rawFilePath.EndsWith(".raw"))
            {
                float[,] heightmap = FileManager.ReadRawHeightmap(rawFilePath, mapData.resolution, true);
                if (mapData.size < 1)
                {
                    Melon<Main>.Logger.Error("Invalid size: Size cant be bellow 1");
                }
                else
                {
                    heightmap = scaleHeightmap(heightmap);
                }

                if (mapData.isSmoothed)
                {
                    heightmap = smoothHeightmap(heightmap, mapData.smoothRadius);
                }

                LoadTerrains(heightmap, terrains);
            }
            else
            {
                Melon<Main>.Logger.Error("Wrong file format");
                return;
            }

            if (File.Exists(treeMaskPath) && main.generateTrees == false)
            {
                bool[,] treeMask = TreeImporter.generateTreeMap(treeMaskPath);
                if (treeMask != null)
                {
                    TreeImporter.setTreesFromMask(terrains, treeMask);
                    return; // If tree mask was used, skip random tree placement.
                }
            }

            int treeAmount;
            if (main.overrideTreeAmount)
                treeAmount = main.treeAmount;
            else
                treeAmount = mapData.treeAmount;

            // Fall back to random tree placement if treeAmount (from GUI) is equal or greater than 0.
            if (treeAmount >= 0)
            {
                TreeImporter.setRandomTrees(terrains, treeAmount);
            }
            else
            {
                Melon<Main>.Logger.Error("Invalid tree amount: Tree amount cant be bellow 0");
            }

        }

        // Smooths the heightmap using a box filter
        private float[,] smoothHeightmap(float[,] heightmap, int areaOfEffect)
        {
            if (areaOfEffect < 1)
            {
                Melon<Main>.Logger.Error("Invalid smoothing radius");
                return null;
            }

            int resolution = (int)Mathf.Sqrt(heightmap.Length);
            float[,] smoothedHeightmap = new float[resolution, resolution];

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float sum = 0;
                    for (int i = -areaOfEffect; i < areaOfEffect; i++)
                    {
                        for (int j = -areaOfEffect; j < areaOfEffect; j++)
                        {
                            if (x + i >= resolution || y + j >= resolution || x + i < 0 || y + j < 0)
                            {
                                continue;
                            }

                            sum += heightmap[x + i, y + j];
                        }
                    }
                    float height = sum / ((areaOfEffect * 2 + 1) * (areaOfEffect * 2 + 1));
                    smoothedHeightmap[x, y] = height;
                }
            }
            return smoothedHeightmap;
        }

        // Scales the heightmap with scale from the mapdata
        private float[,] scaleHeightmap(float[,] heightmap)
        {
            float[,] scaledHeightmap = new float[8200, 8200];
            for (int y = 0; y < mapData.resolution; y++)
            {
                for (int x = 0; x < mapData.resolution; x++)
                {
                    for (int i = 0; i < mapData.size; i++)
                    {
                        for (int j = 0; j < mapData.size; j++)
                        {
                            if (x * mapData.size + i >= 8200 || y * mapData.size + j >= 8200)
                            {
                                continue;
                            }
                            scaledHeightmap[x * mapData.size + i, y * mapData.size + j] = heightmap[x, y];
                        }
                    }
                }
            }
            return scaledHeightmap;
        }

        // Iterates over every terrain and loads the corresponding part of the heightmap
        public void LoadTerrains(float[,] heights, Terrain[,] terrains)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    Terrain currentTerrain = terrains[y, x];

                    currentTerrain.Flush();

                    int resolution = currentTerrain.terrainData.heightmapResolution;
                    int terrainHeigt;
                    if (main.overrideHeight)
                        terrainHeigt = main.heightY;
                    else
                        terrainHeigt = mapData.heightY;
                    Vector3 size = new Vector3(512, terrainHeigt, 512);

                    currentTerrain.terrainData.size = size;
                    currentTerrain.terrainData.heightmapResolution = 1025;

                    float[,] heightmap = DivideHeightMap(heights, resolution, x, y);
                    currentTerrain.terrainData.SetHeights(0, 0, heightmap);

                    currentTerrain.Flush();
                }
            }
        }

        // Returns part of heightmap depending on the resolution and terrain position
        private float[,] DivideHeightMap(float[,] heights, int resolution, int x, int y)
        {
            int totalRes = (int)Mathf.Sqrt(heights.Length);
            float[,] heightmap = new float[resolution, resolution];

            for (int y2 = 0; y2 < resolution; y2++)
            {
                for (int x2 = 0; x2 < resolution; x2++)
                {
                    int globalX = x2 + x * (resolution - 1);    // CHANGE: Overlap tiles by one pixel. 3/17/2025 Chad_Brochill
                    int globalY = y2 + y * (resolution - 1);    // CHANGE: Overlap tiles by one pixel. 3/17/2025 Chad_Brochill

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
    }
}

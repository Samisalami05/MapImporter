using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;

namespace MapImporter
{
    internal class TreeImporter : MelonMod
    {
        // ADDED: New method to place trees based on the tree mask with a corrected vertical orientation.
        public static void setTreesFromMask(Terrain[,] terrains, bool[,] treeMask)
        {
            int globalWidth = treeMask.GetLength(0);
            int globalHeight = treeMask.GetLength(1);
            int tilesX = 8;
            int tilesY = 8;
            int tileWidth = globalWidth / tilesX;
            int tileHeight = globalHeight / tilesY;

            // Loop through each terrain tile.
            for (int tx = 0; tx < tilesX; tx++)
            {
                for (int ty = 0; ty < tilesY; ty++)
                {
                    Terrain terrain = terrains[tx, ty];
                    List<TreeInstance> treeList = new List<TreeInstance>();

                    // Loop through each pixel in the current tile region.
                    for (int localY = 0; localY < tileHeight; localY++)
                    {
                        // Flip the vertical coordinate: correctedLocalY goes from top (tileHeight - 1) to bottom (0)
                        int correctedLocalY = tileHeight - 1 - localY;
                        for (int localX = 0; localX < tileWidth; localX++)
                        {
                            int globalX = tx * tileWidth + localX;
                            int globalY = ty * tileHeight + correctedLocalY;
                            if (treeMask[globalX, globalY])
                            {
                                // Compute normalized position within the terrain tile (0-1), using the flipped Y coordinate.
                                float normX = (float)localX / (tileWidth - 1);
                                float normY = (float)correctedLocalY / (tileHeight - 1);

                                // Convert normalized coordinates to heightmap coordinates.
                                int hmX = Mathf.RoundToInt(normX * (terrain.terrainData.heightmapResolution - 1));
                                int hmY = Mathf.RoundToInt(normY * (terrain.terrainData.heightmapResolution - 1));
                                float heightValue = terrain.terrainData.GetHeight(hmX, hmY);
                                float normHeight = (heightValue - terrain.GetPosition().y) / terrain.terrainData.size.y;

                                TreeInstance tree = new TreeInstance();
                                tree.position = new Vector3(normX, normHeight, normY);
                                tree.widthScale = 1.146039f;
                                tree.heightScale = 1.146039f;
                                tree.prototypeIndex = UnityEngine.Random.Range(0, 3);
                                tree.rotation = UnityEngine.Random.Range(0, (float)Math.PI * 2);
                                treeList.Add(tree);
                            }
                        }
                    }
                    Melon<Main>.Logger.Msg($"Tile [{tx},{ty}] - Trees placed: {treeList.Count}");
                    terrain.terrainData.treeInstances = treeList.ToArray();
                }
            }
        }


        public static void setRandomTrees(Terrain[,] terrains, int treeCount)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    Terrain terrain = terrains[x, y];

                    TreeInstance[] trees = new TreeInstance[treeCount];

                    for (int i = 0; i < trees.Length; i++)
                    {
                        trees[i].widthScale = 1.146039f;
                        trees[i].heightScale = 1.146039f;

                        // Generate random position (normalized)
                        Vector2 treePos = new Vector2(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));

                        // Convert normalized position to world space
                        Vector3 worldPos = terrain.GetPosition() + new Vector3(treePos.x * terrain.terrainData.size.x, 0, treePos.y * terrain.terrainData.size.z);

                        // Convert world position to heightmap coordinates
                        int heightmapX = Mathf.RoundToInt(treePos.x * (terrain.terrainData.heightmapResolution - 1));
                        int heightmapZ = Mathf.RoundToInt(treePos.y * (terrain.terrainData.heightmapResolution - 1));

                        // Get height from terrain data
                        float treeHeight = terrain.terrainData.GetHeight(heightmapX, heightmapZ);

                        // Convert back to normalized position
                        float normalizedHeight = (treeHeight - terrain.GetPosition().y) / terrain.terrainData.size.y;

                        //float treeHeight = terrain.SampleHeight(terrain.GetPosition() + new Vector3(treePos.x * terrain.terrainData.size.x, 0, treePos.y * terrain.terrainData.size.y)) / terrain.terrainData.size.y;

                        trees[i].position = new Vector3(treePos.x, normalizedHeight, treePos.y);
                        trees[i].prototypeIndex = UnityEngine.Random.Range(0, 3);
                        trees[i].rotation = UnityEngine.Random.Range(0, (float)Math.PI * 2);
                    }

                    //Melon<Main>.Logger.Msg("treeCount: " + treeCount + " treePos: " + trees[0].position + " TerrainTreePos: " + terrain.terrainData.treeInstances[0].position);



                    terrains[x, y].terrainData.treeInstances = trees;

                }
            }
        }
    }
}

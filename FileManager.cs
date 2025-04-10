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
    internal class FileManager
    {
        // Returns a list of all files in the given directory
        public static string[] getFiles(string path)
        {
            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path);
                return files;
            }

            return null;
        }

        // Reads a PNG file and returns a Texture2D object
        public static Texture2D readPng(string filePath)
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

            return tex;
        }

        // Returns a list of all map folders with their subfiles in the given path
        public static Dictionary<string, (string rawFile, string pngFile)> GetMapFiles(string path)
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

        // Reads a raw file as a heightmap
        public static float[,] ReadRawHeightmap(string filePath, uint resolution, bool is16Bit)
        {
            if (!File.Exists(filePath))
            {
                Melon<Main>.Logger.Msg($"RAW file not found: {filePath}");
                return null;
            }

            byte[] rawData = File.ReadAllBytes(filePath);
            int expectedSize = (int)resolution * (int)resolution * (is16Bit ? 2 : 1);

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

        // Reads a text file and returns a heightmap
        public static float[,] ReadTxtFile(string filePath)
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


    }
}

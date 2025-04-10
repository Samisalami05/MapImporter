using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace MapImporter
{
    internal class MapData
    {
        public uint resolution;
        public bool is16bit;
        public uint size;
        public bool isSmoothed;
        public int smoothRadius;

        public MapData(uint resolution, bool is16bit, uint size, bool isSmoothed, int smoothRadius)
        {
            this.resolution = resolution;
            this.is16bit = is16bit;
            this.size = size;
            this.isSmoothed = isSmoothed;
            this.smoothRadius = smoothRadius;
        }

        // Reads a JSON file and returns a MapData object
        public static MapData ReadJson(string configsFolderPath, string configName)
        {
            string path = Path.Combine(configsFolderPath, configName + ".json");
            if (File.Exists(path))
            {
                string text = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<MapData>(text);
            }
            return null; // Return null if the file doesn't exist
        }

        // Creates a new JSON file with the given data
        public static void CreateJson(MapData data ,string filepath)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented); // Pretty-print JSON
            File.WriteAllText(filepath, json);
        }
    }
}

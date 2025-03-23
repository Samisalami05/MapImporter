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

        public static MapData CreateMap(uint resolution, bool is16bit, uint size, bool isSmoothed, int smoothRadius)
        {
            MapData map = new MapData
            {
                resolution = resolution,
                is16bit = is16bit,
                size = size,
                isSmoothed = isSmoothed,
                smoothRadius = smoothRadius
            };
            return map;
        }

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

        public static void CreateJson(MapData data ,string filepath)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented); // Pretty-print JSON
            File.WriteAllText(filepath, json);
        }
    }
}

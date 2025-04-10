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
        public string path = "Mods/Maps/";

        public Dictionary<string, (string, string)> mapFiles;

        public bool menuOpen = false;

        public int mapIndex = 0;

        public bool generateTrees = false;

        public int heightY = 4096;
        public int treeAmount = 0;

        private Texture2D defaultMiniMap;

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Garage")
            {
                GameObject mainMenu = GameObject.Find("MainMenu");

                menuOpen = true;

                mapFiles = FileManager.GetMapFiles(path);

                return;
            }

            menuOpen = false;

            if (sceneName == "Idaho")
            {
                
                if (mapIndex <= 0)
                {
                    setMiniMap(defaultMiniMap);
                    
                    return;
                }

                var mapList = mapFiles.ToList();

                string mapPath = mapList[mapIndex - 1].Key;

                MapData data = MapData.ReadJson(mapPath, "config");
                Map map = new Map(this, data);

                changeMiniMap(mapPath);

                if (data == null)
                {
                    Melon<Main>.Logger.Error("Config file could not be found in " + mapPath);
                    return;
                }

                map.loadMap(mapList[mapIndex - 1].Value.Item1, mapList[mapIndex - 1].Value.Item2);

                mapIndex = 0;
            }
        }

        public override void OnInitializeMelon()
        {
            Ui ui = new Ui(this);
            MelonEvents.OnGUI.Subscribe(ui.DrawMenu, 100);
        }

        // Test: changes the minimap (does not work)
        void changeMiniMap(string mapPath)
        {
            if (Directory.Exists(mapPath + "/minimap") == false)
            {
                Melon<Main>.Logger.Msg("Loading terrain without minimap");
                setMiniMap(defaultMiniMap);
                
                return;
            }
            string minimapPath = Directory.GetFiles(mapPath + "/minimap", "*.png").FirstOrDefault();

            Texture2D minimap = FileManager.readPng(minimapPath);

            if (minimap == null) { return; }

            GameObject mapView = GameObject.Find("LevelEssentials/LevelData");

            if (mapView == null)
            {
                Melon<Main>.Logger.Error("Could not find mapView");
                return;
            }

            LevelDataContainer levelDataContainer = mapView.GetComponent<LevelDataContainer>();

            LevelData levelData = levelDataContainer.levelData;

            if (defaultMiniMap == null)
            {
                defaultMiniMap = levelData.mapBackground;
            }

            if (levelData != null)
            {
                levelData.mapBackground = minimap;
            }

            GameObject hud = GameObject.Find("LevelEssentials/Hud");
            if (hud.activeSelf == true)
            {
                hud.SetActive(false);
                hud.SetActive(true);
            }
        }

        private void setMiniMap(Texture2D texture)
        {
            if (texture == null) { return; }

            GameObject mapView = GameObject.Find("LevelEssentials/LevelData");

            if (mapView == null)
            {
                Melon<Main>.Logger.Error("Could not find mapView");
                return;
            }

            LevelDataContainer levelDataContainer = mapView.GetComponent<LevelDataContainer>();

            LevelData levelData = levelDataContainer.levelData;

            if (levelData != null)
            {
                levelData.mapBackground = texture;
            }

            GameObject hud = GameObject.Find("LevelEssentials/Hud");
            if (hud.activeSelf == true)
            {
                hud.SetActive(false);
                hud.SetActive(true);
            }
        }
    }
}
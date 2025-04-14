using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MapImporter
{
    internal class Ui
    {
        private Main main;
        private Rect menuRect = new Rect(Screen.width - 310, 10, 300, 500); // Initial position and size
        private Vector2 scrollPosition = Vector2.zero; // Scroll position

        public Ui(Main main) 
        {
            this.main = main;
        }

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
            labelStyle.alignment = TextAnchor.MiddleLeft;
            labelStyle.margin.left = 20;


            Rect backgroundRect = new Rect(size.x / 2 - width / 2 + offsetWidth, offsetTop + (height + offsetBetween) * index, width, height);
            Rect labelRect = new Rect(size.x / 2 - width / 2 + offsetWidth + 10, offsetTop + (height + offsetBetween) * index, width / 3 * 2, height);
            Rect buttonRect = new Rect(size.x / 2 + backgroundRect.width / 5 - 10 + offsetWidth, offsetTop + (height + offsetBetween) * index + height / 2 - 10, 80, 20);
            Rect toggleRect = new Rect(10f + offsetWidth, offsetTop + (height + offsetBetween) * index, 10, 10);

            GUI.Box(backgroundRect, "");
            GUI.Label(labelRect, name, labelStyle);

            if (GUI.Button(buttonRect, "Load Map"))
            {
                main.mapIndex = index + 1;
                SceneManager.LoadScene(4);
            }
        }

        // Draws the menu window
        private void DrawMenuWindow(int windowID)
        {
            GUIStyle centeredStyle = new GUIStyle(GUI.skin.label);
            centeredStyle.alignment = TextAnchor.MiddleCenter;

            Vector2Int menuSize = new Vector2Int((int)menuRect.width, (int)menuRect.height);

            GUI.Label(new Rect(20, 30, 200, 20), "Terrain Height: ");
            main.heightY = int.Parse(GUI.TextField(new Rect(menuSize.x - 80, 30, 60, 20), main.heightY.ToString()));
            main.overrideHeight = GUI.Toggle(new Rect(menuSize.x - 100, 30, 20, 20), main.overrideHeight, "");
            GUI.Label(new Rect(20, 50, 200, 20), "Tree Amount: ");
            main.treeAmount = int.Parse(GUI.TextField(new Rect(menuSize.x - 80, 50, 60, 20), main.treeAmount.ToString()));
            main.generateTrees = GUI.Toggle(new Rect(menuSize.x - 100, 50, 20, 20), main.generateTrees, "");
            GUI.Label(new Rect(20, 70, 200, 20), "Override Tree Amount: ");
            main.overrideTreeAmount = GUI.Toggle(new Rect(menuSize.x - 40, 70, 20, 20), main.overrideTreeAmount, "");

            int contentHeight = (main.mapFiles != null) ? (main.mapFiles.Count * 50 + 20) : 0; // Dynamic height for scrolling

            scrollPosition = GUI.BeginScrollView(
                new Rect(10, 100, menuSize.x - 15, menuSize.y - 150),  // Scroll view area
                scrollPosition,
                new Rect(0, 0, menuSize.x - 35, contentHeight),       // Content area size
                false,                                                // Horizontal scrolling disabled
                true                                                  // Vertical scrolling enabled
            );



            if (main.mapFiles != null)
            {
                int i = 0;
                foreach (var kvp in main.mapFiles)
                {
                    DrawMap(kvp.Key.Remove(0, 10), i, menuSize);
                    i++;
                }
            }

            GUI.EndScrollView();

            GUI.Label(new Rect(menuSize.x / 2 - 100, menuSize.y - 40, 200, 40), "Made by Samisalami and Chad_Brochill", centeredStyle);

            // Make the window draggable
            GUI.DragWindow(new Rect(0, 0, menuSize.x, 20));
        }

        // Draws the menu
        public void DrawMenu()
        {
            if (main.menuOpen)
            {
                menuRect = GUI.Window(0, menuRect, DrawMenuWindow, "Terrain Importer");
            }
        }
    }
}

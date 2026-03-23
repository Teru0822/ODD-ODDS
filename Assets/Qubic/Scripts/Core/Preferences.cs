using System;
using System.Linq;
using UnityEngine;

namespace QubicNS
{
    /// <summary> 
    /// Global Preferences for Qubic
    /// </summary>
    [HelpURL("")]
    [Serializable]
    [CreateAssetMenu(fileName = "Preferences", menuName = "Qubic/Preferences", order = 100)]
    public class Preferences : ScriptableObject
    {
        [Header("Debug:")]
        [Tooltip("Automatically rebuild the map when something is changed in editor.")]
        public bool AutoRebuild = true;
        [Tooltip("Highlight map cells in the editor.")]
        public bool HighlightCells = false;
        [Tooltip("Highlight map cells of the selected room in the editor.")]
        public bool HighlightCellsOfSelectedRoom = true;

        [Header("Fast Mode:")]
        [Tooltip("Fast Mode provides faster rebuilding of the map by limiting the area and number of levels that are rebuilt.")]
        public bool FastMode = true;
        [Tooltip("The radius to rebuild (meters).")]
        public int RadiusToRebuild = 15;
        [Tooltip("The floors diapason to rebuild.")]
        public int LevelDeltaToRebuild = 0;
        [Tooltip("Minimum number of map cells to activate Fast Mode.")]
        public int MinMapCellsCountToEnableFastMode = 5000;
        [Tooltip("")]
        public bool AllowToUse3DModelsAsPrefab = true;

        private static Preferences instance;

        public static Preferences Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<Preferences>("Preferences");
                return instance;
            }
        }

        public static void Reset()
        {
            instance = new Preferences();
        }
    }
}
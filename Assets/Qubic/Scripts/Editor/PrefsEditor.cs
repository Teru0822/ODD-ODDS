using UnityEditor;
using UnityEngine;

namespace QubicNS
{
    public static class PrefsEditor
    {
        private static Vector2 scrollPosition;
        private static bool forceSave;
        public const string Path = "Project/Qubic";

        [SettingsProvider]
        public static SettingsProvider GetSettingsProvider()
        {
            SettingsProvider provider = new SettingsProvider(Path, SettingsScope.Project)
            {
                label = "Qubic",
                guiHandler = DrawGeneralManagers,
                keywords = new string[] { "Qubic", "Qubic" }
            };
            return provider;
        }

        public static void DrawGeneralManagers(string searchContext)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            try
            {
                EditorGUI.BeginChangeCheck();
                Draw();
                EditorGUILayout.Space();
                if (EditorGUI.EndChangeCheck() || forceSave)
                {
                    EditorUtility.SetDirty(Preferences.Instance);
                    forceSave = false;
                }
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch
            {
            }

            EditorGUILayout.EndScrollView();
        }

        private static void Draw()
        {
            var prefs = Preferences.Instance;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            prefs.AutoRebuild = EditorGUILayout.Toggle("Auto Rebuild", prefs.AutoRebuild);
            prefs.HighlightCells = EditorGUILayout.Toggle("Highlight Cells", prefs.HighlightCells);
            prefs.HighlightCellsOfSelectedRoom = EditorGUILayout.Toggle("Highlight Cells Of Selected Room", prefs.HighlightCellsOfSelectedRoom);
            prefs.AllowToUse3DModelsAsPrefab = EditorGUILayout.Toggle(new GUIContent("Allow To Use 3D Models As Prefab", "Allow To Use 3D Models As Prefab"), prefs.AllowToUse3DModelsAsPrefab);
            prefs.FastMode = EditorGUILayout.Toggle(new GUIContent("Fast Mode", "This mode dynamically rebuilds the building only around the selected room.\r\nThis allows to reduce the rebuild time dramatically and make it independent of the map size.\r\nPressing F5 or the Rebuild button - always builds the entire building."), prefs.FastMode);

            if (prefs.FastMode)
            {
                EditorGUI.indentLevel++;
                prefs.RadiusToRebuild = EditorGUILayout.IntField(new GUIContent("Radius to Rebuild", "Rebuild radius around selected room (in cells).\r\nThis only makes sense for FastMode."), prefs.RadiusToRebuild);
                prefs.LevelDeltaToRebuild = EditorGUILayout.IntField(new GUIContent("Max Level Difference to Rebuild", "The maximum spread of floors that will be built around the first floor of the selected room.\r\nThis only makes sense for FastMode."), prefs.LevelDeltaToRebuild);
                prefs.MinMapCellsCountToEnableFastMode = EditorGUILayout.IntField(new GUIContent("Min Map Cells Count", "The minimum number of cells on the map for FastMode to be enabled."), prefs.MinMapCellsCountToEnableFastMode);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Reset to Default"))
            {
                try
                {
                    Preferences.Reset();
                }catch {}
                forceSave = true;
            }
        }
    }
}

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace QubicNS
{
    static class MenuManager
    {
        const string GameObject = "GameObject/Qubic/";
        const string ComponentMicroWorld = "Component/Qubic/";
        public const string MainMenu = "Tools/Qubic/";

        [Shortcut("Custom/Isomteric view for debug", KeyCode.F6)]
        private static void OnF6Pressed()
        {
            if (QubicNS.QubicBuilder.LastSelectedBuilder != null)
            {
                QubicNS.QubicBuilder.LastSelectedBuilder.Debug.ForcedIsometricView = !QubicNS.QubicBuilder.LastSelectedBuilder.Debug.ForcedIsometricView;
                QubicNS.QubicBuilder.LastSelectedBuilder.RebuildNeeded();
            }
        }

        [MenuItem(MainMenu + "Build _F5", priority = 5, secondaryPriority = 0)]
        static void BuildFromMenu()
        {
            if (Selection.activeObject is GameObject go)
            {
                var world = go.GetComponentInParent<QubicBuilder>(true);
                if (world)
                {
                    world.BuildInEditor(3);
                    EditorUtility.SetDirty(world.gameObject);
                    return;
                } 
            }

            BuildAllFromMenu();
        }

        [MenuItem(MainMenu + "Build All Qubics on Scene", priority = 5, secondaryPriority = 1)]
        static void BuildAllFromMenu()
        {
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                foreach(var builder in scene.GetRootGameObjects().Where(go => go.activeInHierarchy).Select(go => go.GetComponent<QubicBuilder>()).Where(m => m != null && m.enabled))
                {
                    builder.BuildInEditor(3);
                    UnityEditor.EditorUtility.SetDirty(builder.gameObject);
                }
            }
        }

        [MenuItem(MainMenu + "Create Qubic Builder", priority = 5, secondaryPriority = 2)]
        static void QubicBuilder()
        {
            var lastSpawned = QubicNS.QubicBuilder.AllEnabledBuilders.LastOrDefault();

            var go = new GameObject("Qubic", typeof(QubicBuilder));
            var mw = go.GetComponent<QubicBuilder>();
            mw.Seed = Random.Range(0, 100000);
            mw.GrabFrom(lastSpawned);
            Selection.activeObject = mw;

            var room = Templates.Room();
            room.transform.SetParent(mw.transform, false);
            mw.BuildInEditor(3);
        }


        [MenuItem(MainMenu + "Prefab Manager", secondaryPriority = 3)]
        static void ShowPrefabManager()
        {
            PrefabManager.ShowWindow();
        }

        [MenuItem(MainMenu + "Preferences", secondaryPriority = 4)]
        public static void Prefs()
        {
            SettingsService.OpenProjectSettings(PrefsEditor.Path);
        }
    }
}
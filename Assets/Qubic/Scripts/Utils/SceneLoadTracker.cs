#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
#endif

namespace QubicNS
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class SceneLoadTracker
    {
        static bool _SceneIsOpening;
        public static bool SceneIsOpening => _SceneIsOpening || UnityEngine.Time.frameCount == 0;

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void OnProjectLoadedInEditor()
        {
            _SceneIsOpening = true;
            EditorSceneManager.sceneOpening -= EditorSceneManager_sceneOpening;
            EditorSceneManager.sceneOpening += EditorSceneManager_sceneOpening;
            EditorApplication.update += WatchFirstFrame;
        }

        private static void EditorSceneManager_sceneOpening(string path, OpenSceneMode mode)
        {
            _SceneIsOpening = true;
            EditorApplication.update += WatchFirstFrame;
        }

        private static void WatchFirstFrame()
        {
            _SceneIsOpening = false;
            EditorApplication.update -= WatchFirstFrame;
        }
#endif
    }
}

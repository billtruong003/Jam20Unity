using UnityEngine;
using UnityEngine.SceneManagement;

namespace CleanCode.SceneManagement
{
    /// <summary>
    /// A data structure that defines a request to load a scene.
    /// This encapsulates all parameters for a scene transition.
    /// </summary>
    public struct SceneLoadRequest
    {
        public readonly string SceneName;
        public readonly LoadSceneMode LoadMode;
        public readonly CanvasGroup FadeCanvasGroup;
        public readonly float FadeDuration;

        public SceneLoadRequest(
            string sceneName,
            LoadSceneMode loadMode = LoadSceneMode.Single,
            CanvasGroup fadeCanvasGroup = null,
            float fadeDuration = 0.5f)
        {
            SceneName = sceneName;
            LoadMode = loadMode;
            FadeCanvasGroup = fadeCanvasGroup;
            FadeDuration = fadeDuration;
        }
    }
}
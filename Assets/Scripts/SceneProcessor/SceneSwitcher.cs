using UnityEngine;
using UnityEngine.SceneManagement;

namespace CleanCode.SceneManagement
{
    /// <summary>
    /// A versatile utility component to trigger scene transitions.
    /// Can be configured in the Inspector to load specific scenes, reload the current one,
    /// use different load modes, and optionally apply a fade transition.
    /// </summary>
    public class SceneSwitcher : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [Tooltip("The name of the scene to load. Leave empty if this switcher is for reloading.")]
        [SerializeField] private string sceneToLoad;
        [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;

        [Header("Transition Effect (Optional)")]
        [Tooltip("The CanvasGroup to use for the fade effect. If null, no fade will occur.")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        /// <summary>
        /// Loads the scene configured in the 'Scene To Load' field.
        /// To be used by UI events like Button's OnClick.
        /// </summary>
        public void LoadConfiguredScene()
        {
            if (string.IsNullOrEmpty(sceneToLoad))
            {
                Debug.LogError("Scene name is not specified. Cannot load configured scene.");
                return;
            }

            var request = new SceneLoadRequest(sceneToLoad, loadMode, fadeCanvasGroup, fadeDuration);
            SceneLoader.Instance.LoadScene(request);
        }

        /// <summary>
        /// Reloads the currently active scene.
        /// To be used by UI events like Button's OnClick for a 'Restart Level' button.
        /// </summary>
        public void ReloadCurrentScene()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            var request = new SceneLoadRequest(currentSceneName, LoadSceneMode.Single, fadeCanvasGroup, fadeDuration);
            SceneLoader.Instance.LoadScene(request);
        }

        /// <summary>
        /// A dynamic method to load any scene by name, callable from other scripts.
        /// </summary>
        /// <param name="targetSceneName">The name of the scene to load.</param>
        public void LoadSceneByName(string targetSceneName)
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogError("Target scene name cannot be null or empty.");
                return;
            }

            var request = new SceneLoadRequest(targetSceneName, loadMode, fadeCanvasGroup, fadeDuration);
            SceneLoader.Instance.LoadScene(request);
        }

        public void QuitApp()
        {
            Application.Quit();
        }
    }
}
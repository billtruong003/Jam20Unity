using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CleanCode.SceneManagement
{
    /// <summary>
    /// A persistent Singleton that handles the execution of scene load requests.
    /// It processes requests asynchronously and is decoupled from the UI.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        private bool isLoading = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Initiates a scene load based on the provided request.
        /// </summary>
        public void LoadScene(SceneLoadRequest request)
        {
            if (isLoading) return;

            // [FIX] Reset game state trước khi load — tránh scene mới bị đóng băng
            // Game Over / Pause set timeScale = 0 → phải reset về 1 trước khi chuyển scene
            Time.timeScale = 1f;
            AudioListener.pause = false;

            ProcessLoadRequestAsync(request);
        }

        private async void ProcessLoadRequestAsync(SceneLoadRequest request)
        {
            isLoading = true;

            try
            {
                if (request.FadeCanvasGroup != null)
                {
                    await Fade(request.FadeCanvasGroup, 1f, request.FadeDuration);
                }

                AsyncOperation sceneLoadOperation = SceneManager.LoadSceneAsync(request.SceneName, request.LoadMode);
                sceneLoadOperation.allowSceneActivation = false;

                while (sceneLoadOperation.progress < 0.9f)
                {
                    await Task.Yield();
                }

                sceneLoadOperation.allowSceneActivation = true;

                while (!sceneLoadOperation.isDone)
                {
                    await Task.Yield();
                }

                if (request.FadeCanvasGroup != null)
                {
                    await Fade(request.FadeCanvasGroup, 0f, request.FadeDuration);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SceneLoader] Failed to load scene '{request.SceneName}': {e.Message}");
            }
            finally
            {
                // [FIX] LUÔN reset isLoading — nếu không, mọi lần gọi sau đều bị reject
                isLoading = false;
            }
        }

        private async Task Fade(CanvasGroup canvasGroup, float targetAlpha, float duration)
        {
            canvasGroup.blocksRaycasts = true;
            float time = 0;
            float startAlpha = canvasGroup.alpha;

            while (time < duration)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
                // [FIX] unscaledDeltaTime — fade chạy đúng kể cả khi timeScale vừa được reset
                time += Time.unscaledDeltaTime;
                await Task.Yield();
            }

            canvasGroup.alpha = targetAlpha;
            canvasGroup.blocksRaycasts = targetAlpha >= 1f;
        }
    }
}
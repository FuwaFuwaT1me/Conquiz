using UnityEngine;

namespace Conquiz.Core
{
    /// <summary>
    /// Automatically loads the MenuScene when BootScene starts.
    /// Ensures SceneLoader is initialized before loading.
    /// </summary>
    public class BootstrapLoader : MonoBehaviour
    {
        [SerializeField] private string menuSceneName = "MenuScene";
        [SerializeField] private float delaySeconds = 0.1f;

        void Start()
        {
            // Small delay to ensure SceneLoader is initialized
            Invoke(nameof(LoadMenu), delaySeconds);
        }

        private void LoadMenu()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene(menuSceneName);
            }
            else
            {
                Debug.LogError("SceneLoader instance not found in BootstrapLoader!");
            }
        }
    }
}

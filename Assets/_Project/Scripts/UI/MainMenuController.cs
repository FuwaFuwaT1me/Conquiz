using UnityEngine;
using UnityEngine.UI;
using Conquiz.Core;

namespace Conquiz.UI
{
    /// <summary>
    /// Controls the main menu UI and navigation.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button startVsBotButton;

        [Header("Scene Settings")]
        [SerializeField] private string gameSceneName = "GameScene";

        void Start()
        {
            // Subscribe to button click
            if (startVsBotButton != null)
            {
                startVsBotButton.onClick.AddListener(OnStartVsBotClicked);
            }
            else
            {
                Debug.LogError("StartVsBot button not assigned in MainMenuController!");
            }
        }

        void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            if (startVsBotButton != null)
            {
                startVsBotButton.onClick.RemoveListener(OnStartVsBotClicked);
            }
        }

        private void OnStartVsBotClicked()
        {
            Debug.Log("Starting game vs Bot...");

            // Ensure SceneLoader exists
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene(gameSceneName);
            }
            else
            {
                Debug.LogError("SceneLoader instance not found! Make sure SceneLoader exists in the scene.");
            }
        }
    }
}

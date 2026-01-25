using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Conquiz.UI
{
    /// <summary>
    /// Controls the in-game HUD, displaying player info and turn controls.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private Button endTurnButton;

        [Header("Settings")]
        [SerializeField] private string defaultPlayerName = "Player 1";

        private string _currentPlayerName;

        void Start()
        {
            // Set default player name
            SetPlayerName(defaultPlayerName);

            // Subscribe to button click
            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }
            else
            {
                Debug.LogError("End Turn button not assigned in GameHUD!");
            }
        }

        void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
            }
        }

        /// <summary>
        /// Updates the displayed player name.
        /// </summary>
        public void SetPlayerName(string playerName)
        {
            _currentPlayerName = playerName;

            if (playerNameText != null)
            {
                playerNameText.text = _currentPlayerName;
            }
            else
            {
                Debug.LogError("Player name text not assigned in GameHUD!");
            }
        }

        /// <summary>
        /// Gets the current player name.
        /// </summary>
        public string GetCurrentPlayerName()
        {
            return _currentPlayerName;
        }

        private void OnEndTurnClicked()
        {
            Debug.Log($"{_currentPlayerName} ended their turn.");

            // TODO: Integrate with game turn system when implemented
            // For now, just demonstrate functionality by toggling player name
            DemoTogglePlayer();
        }

        /// <summary>
        /// Demo function to toggle between players (remove when game manager is implemented).
        /// </summary>
        private void DemoTogglePlayer()
        {
            if (_currentPlayerName == "Player 1")
            {
                SetPlayerName("Bot Player");
            }
            else
            {
                SetPlayerName("Player 1");
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Conquiz.UI
{
    public enum BadgeState
    {
        Hidden,
        Thinking,
        Answered,
        TimedOut,
        ResultCorrect,
        ResultWrong
    }

    /// <summary>
    /// Displays real-time status for a player during quiz sessions.
    /// Shows player label and status text.
    /// Auto-finds child components if not assigned.
    /// </summary>
    public class PlayerStatusBadge : MonoBehaviour
    {
        [Header("UI References (Auto-found if null)")]
        [SerializeField] private TextMeshProUGUI playerLabel;
        [SerializeField] private Image timerRing;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image background;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Colors")]
        [SerializeField] private Color playerColor = Color.cyan;
        [SerializeField] private Color thinkingColor = new Color(0.7f, 0.7f, 0.8f);
        [SerializeField] private Color answeredColor = new Color(0.4f, 0.9f, 0.5f);
        [SerializeField] private Color correctColor = new Color(0.2f, 0.85f, 0.4f);
        [SerializeField] private Color wrongColor = new Color(0.94f, 0.35f, 0.35f);
        [SerializeField] private Color timedOutColor = new Color(0.9f, 0.5f, 0.2f);

        [Header("Timer Colors")]
        [SerializeField] private Color timerColorFull = new Color(0.23f, 0.51f, 0.96f);
        [SerializeField] private Color timerColorMid = new Color(0.96f, 0.62f, 0.11f);
        [SerializeField] private Color timerColorLow = new Color(0.94f, 0.27f, 0.27f);

        [Header("Timer Thresholds")]
        [SerializeField] private float timerMidThreshold = 0.5f;
        [SerializeField] private float timerLowThreshold = 0.25f;

        private BadgeState currentState = BadgeState.Thinking;
        private bool isInitialized = false;

        void Awake()
        {
            AutoFindComponents();
            isInitialized = true;
        }

        void Start()
        {
            // Set initial state to Thinking
            if (currentState == BadgeState.Hidden)
            {
                SetState(BadgeState.Thinking);
            }
        }

        private void AutoFindComponents()
        {
            // Auto-find background image
            if (background == null)
            {
                background = GetComponent<Image>();
            }

            // Auto-find child text components
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                if (text.gameObject.name == "Label" && playerLabel == null)
                {
                    playerLabel = text;
                }
                else if (text.gameObject.name == "Status" && statusText == null)
                {
                    statusText = text;
                }
            }
            
            Debug.Log($"[PlayerStatusBadge] {gameObject.name} - Auto-find: playerLabel={playerLabel != null}, statusText={statusText != null}");

            // Setup timer ring if found
            if (timerRing != null)
            {
                timerRing.type = Image.Type.Filled;
                timerRing.fillMethod = Image.FillMethod.Radial360;
                timerRing.fillOrigin = (int)Image.Origin360.Top;
                timerRing.fillClockwise = false;
            }
        }

        public void Initialize(string label, Color color)
        {
            if (!isInitialized) AutoFindComponents();
            
            if (playerLabel != null)
                playerLabel.text = label;

            playerColor = color;
            
            if (playerLabel != null)
                playerLabel.color = color;
        }

        public void SetState(BadgeState state)
        {
            if (!isInitialized) AutoFindComponents();
            
            currentState = state;
            Debug.Log($"[PlayerStatusBadge] {gameObject.name} -> SetState({state}), statusText={statusText != null}");

            switch (state)
            {
                case BadgeState.Hidden:
                    if (canvasGroup != null)
                        canvasGroup.alpha = 0f;
                    else
                        gameObject.SetActive(false);
                    break;

                case BadgeState.Thinking:
                    if (canvasGroup != null)
                        canvasGroup.alpha = 1f;
                    else
                        gameObject.SetActive(true);
                        
                    if (statusText != null)
                    {
                        statusText.text = "Thinking...";
                        statusText.color = thinkingColor;
                        statusText.fontStyle = FontStyles.Italic;
                    }
                    break;

                case BadgeState.Answered:
                    if (statusText != null)
                    {
                        statusText.text = "Answered!";
                        statusText.color = answeredColor;
                        statusText.fontStyle = FontStyles.Bold;
                    }
                    Debug.Log($"[PlayerStatusBadge] {gameObject.name} -> ANSWERED");
                    break;

                case BadgeState.TimedOut:
                    if (statusText != null)
                    {
                        statusText.text = "Timed Out";
                        statusText.color = timedOutColor;
                        statusText.fontStyle = FontStyles.Bold;
                    }
                    break;

                case BadgeState.ResultCorrect:
                    if (statusText != null)
                    {
                        statusText.text = "Correct! ✓";
                        statusText.color = correctColor;
                        statusText.fontStyle = FontStyles.Bold;
                    }
                    break;

                case BadgeState.ResultWrong:
                    if (statusText != null)
                    {
                        statusText.text = "Wrong ✗";
                        statusText.color = wrongColor;
                        statusText.fontStyle = FontStyles.Bold;
                    }
                    break;
            }
        }

        public void UpdateTimer(float normalizedTime)
        {
            if (timerRing != null)
            {
                timerRing.fillAmount = normalizedTime;

                // Color gradient based on time remaining
                if (normalizedTime > timerMidThreshold)
                    timerRing.color = timerColorFull;
                else if (normalizedTime > timerLowThreshold)
                    timerRing.color = Color.Lerp(timerColorMid, timerColorFull, 
                        (normalizedTime - timerLowThreshold) / (timerMidThreshold - timerLowThreshold));
                else
                    timerRing.color = Color.Lerp(timerColorLow, timerColorMid, 
                        normalizedTime / timerLowThreshold);
            }
        }

        public void Reset()
        {
            SetState(BadgeState.Thinking);
            UpdateTimer(1f);
        }
        
        public BadgeState CurrentState => currentState;
    }
}

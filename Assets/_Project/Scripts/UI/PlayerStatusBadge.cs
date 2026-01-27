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
    /// Shows player label, circular timer, and status text.
    /// </summary>
    public class PlayerStatusBadge : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI playerLabel;
        [SerializeField] private Image timerRing;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image background;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Colors")]
        [SerializeField] private Color playerColor = Color.cyan;
        [SerializeField] private Color timerColorFull = new Color(0.23f, 0.51f, 0.96f); // Blue
        [SerializeField] private Color timerColorMid = new Color(0.96f, 0.62f, 0.11f); // Yellow
        [SerializeField] private Color timerColorLow = new Color(0.94f, 0.27f, 0.27f); // Red
        [SerializeField] private Color correctColor = new Color(0.06f, 0.72f, 0.51f);
        [SerializeField] private Color wrongColor = new Color(0.94f, 0.27f, 0.27f);

        [Header("Timer Thresholds")]
        [SerializeField] private float timerMidThreshold = 0.5f;
        [SerializeField] private float timerLowThreshold = 0.25f;

        [Header("Visual Settings")]
        [SerializeField] private float thinkingBackgroundAlpha = 0.1f;

        [Header("Status Symbols")]
        [SerializeField] private string answeredSymbol = "✓";
        [SerializeField] private string correctSymbol = "✓";
        [SerializeField] private string wrongSymbol = "✗";

        private BadgeState currentState = BadgeState.Hidden;

        void Awake()
        {
            if (timerRing != null)
            {
                timerRing.type = Image.Type.Filled;
                timerRing.fillMethod = Image.FillMethod.Radial360;
                timerRing.fillOrigin = (int)Image.Origin360.Top;
                timerRing.fillClockwise = false;
            }

            SetState(BadgeState.Hidden);
        }

        public void Initialize(string label, Color color)
        {
            if (playerLabel != null)
                playerLabel.text = label;

            playerColor = color;
        }

        public void SetState(BadgeState state)
        {
            currentState = state;

            switch (state)
            {
                case BadgeState.Hidden:
                    if (canvasGroup != null)
                        canvasGroup.alpha = 0f;
                    break;

                case BadgeState.Thinking:
                    if (canvasGroup != null)
                        canvasGroup.alpha = 1f;
                    if (statusText != null)
                        statusText.text = "Thinking...";
                    if (background != null)
                        background.color = new Color(1f, 1f, 1f, thinkingBackgroundAlpha);
                    break;

                case BadgeState.Answered:
                    if (statusText != null)
                        statusText.text = $"Answered! {answeredSymbol}";
                    break;

                case BadgeState.TimedOut:
                    if (statusText != null)
                        statusText.text = "Time's up!";
                    if (timerRing != null)
                        timerRing.color = timerColorLow;
                    break;

                case BadgeState.ResultCorrect:
                    if (statusText != null)
                        statusText.text = correctSymbol;
                    if (background != null)
                        background.color = correctColor;
                    break;

                case BadgeState.ResultWrong:
                    if (statusText != null)
                        statusText.text = wrongSymbol;
                    if (background != null)
                        background.color = wrongColor;
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
                    timerRing.color = Color.Lerp(timerColorMid, timerColorFull, (normalizedTime - timerLowThreshold) / (timerMidThreshold - timerLowThreshold));
                else
                    timerRing.color = Color.Lerp(timerColorLow, timerColorMid, normalizedTime / timerLowThreshold);
            }
        }

        public void Reset()
        {
            SetState(BadgeState.Thinking);
            UpdateTimer(1f);
        }
    }
}

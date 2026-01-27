using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Conquiz.Quiz;

namespace Conquiz.UI
{
    /// <summary>
    /// Controls the quiz UI for displaying MCQ and Numeric questions.
    /// Handles timer countdown, answer display, and round results.
    /// </summary>
    public class QuizUIController : MonoBehaviour
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        public event Action<int, float> OnMcqAnswerSubmitted;
        public event Action<float, float> OnNumericAnswerSubmitted;
        public event Action OnTimerExpired;

        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Panels")]
        [SerializeField] private GameObject quizPanel;
        [SerializeField] private GameObject mcqPanel;
        [SerializeField] private GameObject numericPanel;
        [SerializeField] private GameObject resultsPanel;

        [Header("Player Status Badges")]
        [SerializeField] private PlayerStatusBadge playerBadgeLeft;
        [SerializeField] private PlayerStatusBadge playerBadgeRight;

        [Header("Reveal Elements")]
        [SerializeField] private GameObject playerLabelPrefab; // Small chip prefab for "YOU"/"OPPONENT"

        [Header("Question Display")]
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private TextMeshProUGUI categoryText;
        [SerializeField] private TextMeshProUGUI roundIndicatorText;

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image timerFillImage;
        [SerializeField] private float defaultTimeLimit = 15f;

        [Header("MCQ Elements")]
        [SerializeField] private Button[] mcqButtons = new Button[4];
        [SerializeField] private TextMeshProUGUI[] mcqButtonTexts = new TextMeshProUGUI[4];
        [SerializeField] private Image[] mcqButtonImages = new Image[4];

        [Header("Numeric Elements")]
        [SerializeField] private TMP_InputField numericInputField;
        [SerializeField] private Button numericSubmitButton;
        [SerializeField] private TextMeshProUGUI unitText;
        [SerializeField] private TextMeshProUGUI rangeHintText;

        [Header("Results Display")]
        [SerializeField] private TextMeshProUGUI playerResultText;
        [SerializeField] private TextMeshProUGUI opponentResultText;
        [SerializeField] private TextMeshProUGUI roundOutcomeText;

        [Header("Modern Colors")]
        [SerializeField] private Color primaryColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color accentColor = new Color(0.4f, 0.8f, 0.4f);
        [SerializeField] private Color correctColor = new Color(0.3f, 0.85f, 0.4f);
        [SerializeField] private Color incorrectColor = new Color(1f, 0.4f, 0.4f);
        [SerializeField] private Color warningColor = new Color(1f, 0.6f, 0.2f);
        [SerializeField] private Color neutralColor = new Color(0.3f, 0.35f, 0.45f);

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private QuestionData currentQuestion;
        private float timeRemaining;
        private float timeLimit;
        private float questionStartTime;
        private bool isTimerRunning;
        private bool hasAnswered;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            for (int i = 0; i < mcqButtons.Length; i++)
            {
                int index = i;
                if (mcqButtons[i] != null)
                {
                    mcqButtons[i].onClick.AddListener(() => OnMcqButtonClicked(index));
                }
            }

            if (numericSubmitButton != null)
            {
                numericSubmitButton.onClick.AddListener(OnNumericSubmitClicked);
            }

            if (numericInputField != null)
            {
                numericInputField.onSubmit.AddListener(_ => OnNumericSubmitClicked());
            }

            // Initialize player status badges
            if (playerBadgeLeft != null)
                playerBadgeLeft.Initialize("YOU", new Color(0.08f, 0.72f, 0.65f)); // Teal

            if (playerBadgeRight != null)
                playerBadgeRight.Initialize("OPPONENT", new Color(0.98f, 0.45f, 0.09f)); // Orange

            HideQuiz();
        }

        private void Update()
        {
            if (!isTimerRunning) return;

            timeRemaining -= Time.deltaTime;
            UpdateTimerDisplay();

            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                isTimerRunning = false;
                HandleTimerExpired();
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < mcqButtons.Length; i++)
            {
                if (mcqButtons[i] != null)
                    mcqButtons[i].onClick.RemoveAllListeners();
            }

            if (numericSubmitButton != null)
                numericSubmitButton.onClick.RemoveAllListeners();

            if (numericInputField != null)
                numericInputField.onSubmit.RemoveAllListeners();
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        public void ShowMcqQuestion(McqQuestionData question, float customTimeLimit = 0f, string roundText = "Round 1: Multiple Choice")
        {
            if (question == null) return;

            // ADD THIS:
            ClearPlayerLabels();

            currentQuestion = question;
            hasAnswered = false;

            if (questionText != null)
                questionText.text = question.QuestionText;

            if (categoryText != null)
                categoryText.text = question.Category ?? "";

            if (roundIndicatorText != null)
                roundIndicatorText.text = roundText;

            for (int i = 0; i < mcqButtons.Length && i < question.Choices.Length; i++)
            {
                if (mcqButtonTexts[i] != null)
                    mcqButtonTexts[i].text = question.Choices[i];

                if (mcqButtons[i] != null)
                {
                    mcqButtons[i].interactable = true;
                    SetButtonStyle(mcqButtons[i], mcqButtonImages[i], neutralColor, Color.white);
                }
            }

            // Show status badges
            if (playerBadgeLeft != null)
                playerBadgeLeft.SetState(BadgeState.Thinking);

            if (playerBadgeRight != null)
                playerBadgeRight.SetState(BadgeState.Thinking);

            ShowPanel(mcqPanel, true);
            ShowPanel(numericPanel, false);
            ShowPanel(resultsPanel, false);
            ShowPanel(quizPanel, true);

            StartTimer(customTimeLimit > 0f ? customTimeLimit : defaultTimeLimit);
        }

        public void ShowNumericQuestion(NumericQuestionData question, float customTimeLimit = 0f, string roundText = "Round 2: Numeric")
        {
            if (question == null) return;

            currentQuestion = question;
            hasAnswered = false;

            if (questionText != null)
                questionText.text = question.QuestionText;

            if (categoryText != null)
                categoryText.text = question.Category ?? "";

            if (roundIndicatorText != null)
                roundIndicatorText.text = roundText;

            if (unitText != null)
                unitText.text = question.Unit ?? "";

            if (rangeHintText != null)
                rangeHintText.text = $"Range: {question.AllowedRangeMin:N0} - {question.AllowedRangeMax:N0}";

            if (numericInputField != null)
            {
                numericInputField.text = "";
                numericInputField.interactable = true;
                numericInputField.contentType = question.DecimalPlaces > 0
                    ? TMP_InputField.ContentType.DecimalNumber
                    : TMP_InputField.ContentType.IntegerNumber;
                numericInputField.Select();
                numericInputField.ActivateInputField();
            }

            if (numericSubmitButton != null)
                numericSubmitButton.interactable = true;

            // Reset status badges
            if (playerBadgeLeft != null)
                playerBadgeLeft.SetState(BadgeState.Thinking);

            if (playerBadgeRight != null)
                playerBadgeRight.SetState(BadgeState.Thinking);

            ShowPanel(mcqPanel, false);
            ShowPanel(numericPanel, true);
            ShowPanel(resultsPanel, false);
            ShowPanel(quizPanel, true);

            StartTimer(customTimeLimit > 0f ? customTimeLimit : defaultTimeLimit);
        }

        /// <summary>
        /// Animates transition from Round 1 (MCQ) to Round 2 (Numeric).
        /// Total duration: ~1.2-1.5 seconds.
        /// </summary>
        public IEnumerator TransitionToRound2Coroutine(NumericQuestionData numericQuestion, float customTimeLimit = 0f)
        {
            if (numericQuestion == null) yield break;

            // Phase 1: Fade Out (0.4s)
            float fadeOutDuration = 0.4f;
            float elapsed = 0f;

            CanvasGroup mcqCanvasGroup = mcqPanel.GetComponent<CanvasGroup>();
            if (mcqCanvasGroup == null)
                mcqCanvasGroup = mcqPanel.AddComponent<CanvasGroup>();

            CanvasGroup questionCanvasGroup = questionText.GetComponent<CanvasGroup>();
            if (questionCanvasGroup == null)
                questionCanvasGroup = questionText.gameObject.AddComponent<CanvasGroup>();

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                float easedT = 1f - Mathf.Pow(1f - t, 3f); // ease-out-cubic

                mcqCanvasGroup.alpha = 1f - easedT;
                questionCanvasGroup.alpha = 1f - easedT;

                // Scale down slightly
                mcqPanel.transform.localScale = Vector3.one * (1f - easedT * 0.05f);

                yield return null;
            }

            mcqCanvasGroup.alpha = 0f;
            questionCanvasGroup.alpha = 0f;

            // Phase 2: Slide Transition (0.5s)
            // Hide MCQ panel
            ShowPanel(mcqPanel, false);
            ClearPlayerLabels();

            // Prepare numeric panel (off-screen right)
            ShowPanel(numericPanel, true);
            CanvasGroup numericCanvasGroup = numericPanel.GetComponent<CanvasGroup>();
            if (numericCanvasGroup == null)
                numericCanvasGroup = numericPanel.AddComponent<CanvasGroup>();

            RectTransform numericRect = numericPanel.GetComponent<RectTransform>();
            Vector2 startPos = new Vector2(Screen.width, 0f);
            Vector2 endPos = Vector2.zero;

            float slideDuration = 0.5f;
            elapsed = 0f;

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideDuration;
                // ease-in-out-cubic
                float easedT = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

                if (numericRect != null)
                    numericRect.anchoredPosition = Vector2.Lerp(startPos, endPos, easedT);

                yield return null;
            }

            if (numericRect != null)
                numericRect.anchoredPosition = endPos;

            // Phase 3: Fade In (0.3s)
            // Setup numeric question
            currentQuestion = numericQuestion;
            hasAnswered = false;

            if (questionText != null)
                questionText.text = numericQuestion.QuestionText;

            if (categoryText != null)
                categoryText.text = numericQuestion.Category ?? "";

            if (roundIndicatorText != null)
                roundIndicatorText.text = "Round 2: Numeric";

            if (unitText != null)
                unitText.text = numericQuestion.Unit ?? "";

            if (rangeHintText != null)
                rangeHintText.text = $"Range: {numericQuestion.AllowedRangeMin:N0} - {numericQuestion.AllowedRangeMax:N0}";

            if (numericInputField != null)
            {
                numericInputField.text = "";
                numericInputField.interactable = true;
                numericInputField.contentType = numericQuestion.DecimalPlaces > 0
                    ? TMP_InputField.ContentType.DecimalNumber
                    : TMP_InputField.ContentType.IntegerNumber;
            }

            if (numericSubmitButton != null)
                numericSubmitButton.interactable = true;

            // Reset status badges
            if (playerBadgeLeft != null)
                playerBadgeLeft.Reset();

            if (playerBadgeRight != null)
                playerBadgeRight.Reset();

            float fadeInDuration = 0.3f;
            elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                float easedT = 1f - Mathf.Pow(1f - t, 3f); // ease-out-cubic

                numericCanvasGroup.alpha = easedT;
                questionCanvasGroup.alpha = easedT;

                // Scale up slightly
                numericPanel.transform.localScale = Vector3.one * (0.95f + easedT * 0.05f);

                yield return null;
            }

            numericCanvasGroup.alpha = 1f;
            questionCanvasGroup.alpha = 1f;
            numericPanel.transform.localScale = Vector3.one;

            // Start numeric question timer
            StartTimer(customTimeLimit > 0f ? customTimeLimit : defaultTimeLimit);

            if (numericInputField != null)
            {
                numericInputField.Select();
                numericInputField.ActivateInputField();
            }
        }

        /// <summary>
        /// Shows MCQ result with button highlighting.
        /// </summary>
        public void ShowMcqResult(int correctIndex, int playerIndex)
        {
            for (int i = 0; i < mcqButtons.Length; i++)
            {
                if (mcqButtons[i] == null) continue;

                mcqButtons[i].interactable = false;

                if (i == correctIndex)
                {
                    SetButtonStyle(mcqButtons[i], mcqButtonImages[i], correctColor, Color.white);
                }
                else if (i == playerIndex && playerIndex != correctIndex)
                {
                    SetButtonStyle(mcqButtons[i], mcqButtonImages[i], incorrectColor, Color.white);
                }
            }
        }

        /// <summary>
        /// Shows round results comparing both players' answers.
        /// </summary>
        public void ShowRoundResults(
            string playerAnswer,
            bool playerCorrect,
            string opponentAnswer,
            bool opponentCorrect,
            string outcomeMessage)
        {
            ShowPanel(mcqPanel, false);
            ShowPanel(numericPanel, false);
            ShowPanel(resultsPanel, true);

            if (playerResultText != null)
            {
                playerResultText.text = $"<b>YOU:</b> {playerAnswer}";
                playerResultText.color = playerCorrect ? correctColor : incorrectColor;
            }

            if (opponentResultText != null)
            {
                opponentResultText.text = $"<b>OPPONENT:</b> {opponentAnswer}";
                opponentResultText.color = opponentCorrect ? correctColor : incorrectColor;
            }

            if (roundOutcomeText != null)
            {
                roundOutcomeText.text = outcomeMessage;
            }
        }

        /// <summary>
        /// Shows a simple feedback message.
        /// </summary>
        public void ShowFeedback(string message, bool isPositive)
        {
            if (roundOutcomeText != null)
            {
                roundOutcomeText.text = message;
                roundOutcomeText.color = isPositive ? correctColor : incorrectColor;
            }
        }

        public void HideQuiz()
        {
            isTimerRunning = false;
            ShowPanel(quizPanel, false);
        }

        public void StopTimer()
        {
            isTimerRunning = false;
        }

        public float GetCurrentResponseTime()
        {
            return Time.time - questionStartTime;
        }

        /// <summary>
        /// Updates the player badge to "Answered" state.
        /// Called when player submits answer.
        /// </summary>
        public void MarkPlayerAnswered()
        {
            if (playerBadgeLeft != null)
                playerBadgeLeft.SetState(BadgeState.Answered);
        }

        /// <summary>
        /// Updates the opponent badge to "Answered" state.
        /// Called when opponent submits answer.
        /// </summary>
        public void MarkOpponentAnswered()
        {
            if (playerBadgeRight != null)
                playerBadgeRight.SetState(BadgeState.Answered);
        }

        /// <summary>
        /// Shows MCQ reveal with player labels on buttons.
        /// Highlights correct answer and shows who picked what.
        /// </summary>
        public IEnumerator ShowMcqRevealCoroutine(
            int correctIndex,
            int playerChoiceIndex,
            int opponentChoiceIndex,
            bool playerCorrect,
            bool opponentCorrect)
        {
            // Step 1: Brief pause (0.3s)
            yield return new WaitForSeconds(0.3f);

            // Step 2: Highlight answers (0.7s)
            for (int i = 0; i < mcqButtons.Length; i++)
            {
                if (mcqButtons[i] == null) continue;

                mcqButtons[i].interactable = false;

                // Highlight correct answer
                if (i == correctIndex)
                {
                    SetButtonStyle(mcqButtons[i], mcqButtonImages[i], correctColor, Color.white);
                }
                // Show player's wrong answer
                else if (i == playerChoiceIndex && !playerCorrect)
                {
                    SetButtonBorder(mcqButtons[i], incorrectColor, 3f);
                    AddPlayerLabel(mcqButtons[i].transform, "YOU", incorrectColor);
                }
                // Show opponent's wrong answer
                else if (i == opponentChoiceIndex && !opponentCorrect)
                {
                    SetButtonBorder(mcqButtons[i], warningColor, 3f);
                    AddPlayerLabel(mcqButtons[i].transform, "OPPONENT", warningColor);
                }
            }

            yield return new WaitForSeconds(0.5f);

            // Step 3: Update status badges (0.3s)
            if (playerBadgeLeft != null)
                playerBadgeLeft.SetState(playerCorrect ? BadgeState.ResultCorrect : BadgeState.ResultWrong);

            if (playerBadgeRight != null)
                playerBadgeRight.SetState(opponentCorrect ? BadgeState.ResultCorrect : BadgeState.ResultWrong);

            yield return new WaitForSeconds(0.5f);

            // Total time: ~1.5s
        }

        // =====================================================================
        // PRIVATE METHODS
        // =====================================================================

        private void StartTimer(float duration)
        {
            timeLimit = duration;
            timeRemaining = duration;
            questionStartTime = Time.time;
            isTimerRunning = true;
            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(timeRemaining);
                timerText.text = seconds.ToString();
                timerText.color = timeRemaining <= 5f ? warningColor : Color.white;
            }

            if (timerFillImage != null)
            {
                timerFillImage.fillAmount = timeRemaining / timeLimit;
                timerFillImage.color = timeRemaining <= 5f ? warningColor : primaryColor;
            }

            // Update badge timers
            float normalizedTime = timeRemaining / timeLimit;
            if (playerBadgeLeft != null && !hasAnswered)
                playerBadgeLeft.UpdateTimer(normalizedTime);

            if (playerBadgeRight != null)
                playerBadgeRight.UpdateTimer(normalizedTime);
        }

        private void OnMcqButtonClicked(int index)
        {
            if (hasAnswered || !isTimerRunning) return;

            hasAnswered = true;
            isTimerRunning = false;
            MarkPlayerAnswered();

            float responseTime = GetCurrentResponseTime();

            foreach (var btn in mcqButtons)
            {
                if (btn != null)
                    btn.interactable = false;
            }

            OnMcqAnswerSubmitted?.Invoke(index, responseTime);
        }

        private void OnNumericSubmitClicked()
        {
            if (hasAnswered || !isTimerRunning) return;

            if (numericInputField == null || string.IsNullOrWhiteSpace(numericInputField.text))
                return;

            if (!float.TryParse(numericInputField.text, out float value))
                return;

            if (currentQuestion is NumericQuestionData numericQ)
            {
                value = numericQ.ClampToAllowedRange(value);
            }

            hasAnswered = true;
            isTimerRunning = false;
            MarkPlayerAnswered();

            float responseTime = GetCurrentResponseTime();

            if (numericInputField != null)
                numericInputField.interactable = false;
            if (numericSubmitButton != null)
                numericSubmitButton.interactable = false;

            OnNumericAnswerSubmitted?.Invoke(value, responseTime);
        }

        private void HandleTimerExpired()
        {
            hasAnswered = true;

            foreach (var btn in mcqButtons)
            {
                if (btn != null)
                    btn.interactable = false;
            }

            if (numericInputField != null)
                numericInputField.interactable = false;
            if (numericSubmitButton != null)
                numericSubmitButton.interactable = false;

            OnTimerExpired?.Invoke();
        }

        private void ShowPanel(GameObject panel, bool show)
        {
            if (panel != null)
                panel.SetActive(show);
        }

        private void SetButtonStyle(Button button, Image buttonImage, Color bgColor, Color textColor)
        {
            if (button == null) return;

            var colors = button.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.1f;
            colors.pressedColor = bgColor * 0.9f;
            colors.selectedColor = bgColor;
            button.colors = colors;

            if (buttonImage != null)
            {
                buttonImage.color = bgColor;
            }
        }

        private void SetButtonBorder(Button button, Color borderColor, float borderWidth)
        {
            // For MVP, we'll use the button's Outline component
            // In Unity Editor, ensure buttons have an Outline component
            var outline = button.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = borderColor;
                outline.effectDistance = new Vector2(borderWidth, borderWidth);
                outline.enabled = true;
            }
        }

        private void AddPlayerLabel(Transform buttonTransform, string labelText, Color backgroundColor)
        {
            if (playerLabelPrefab == null) return;

            GameObject label = Instantiate(playerLabelPrefab, buttonTransform);
            label.name = $"Label_{labelText}";

            // Position in top-right corner
            RectTransform rectTransform = label.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(1f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(1f, 1f);
                rectTransform.anchoredPosition = new Vector2(-10f, -10f);
            }

            // Set label text and color
            TextMeshProUGUI labelTextComponent = label.GetComponentInChildren<TextMeshProUGUI>();
            if (labelTextComponent != null)
            {
                labelTextComponent.text = labelText;
            }

            Image labelBackground = label.GetComponent<Image>();
            if (labelBackground != null)
            {
                labelBackground.color = backgroundColor;
            }
        }

        private void ClearPlayerLabels()
        {
            foreach (var button in mcqButtons)
            {
                if (button == null) continue;

                // Find and destroy any player labels
                Transform[] children = button.GetComponentsInChildren<Transform>();
                foreach (Transform child in children)
                {
                    if (child.name.StartsWith("Label_"))
                    {
                        Destroy(child.gameObject);
                    }
                }

                // Disable outline
                var outline = button.GetComponent<Outline>();
                if (outline != null)
                    outline.enabled = false;
            }
        }
    }
}

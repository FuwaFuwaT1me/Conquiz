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
        public event Action OnContinueClicked;

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
        [SerializeField] private Button continueButton;
        [SerializeField] private TextMeshProUGUI correctAnswerText;

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
        private bool waitingForContinue;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            SetupButtonListeners();
            InitializePlayerBadges();
            FixupInputFieldViewport();
            
            // Hide ALL panels initially - quiz will be shown when session starts
            ShowPanel(quizPanel, false);
            ShowPanel(mcqPanel, false);
            ShowPanel(numericPanel, false);
            ShowPanel(resultsPanel, false);
            
            if (continueButton != null)
                continueButton.gameObject.SetActive(false);
                
            Debug.Log("[QuizUI] Initialized - panels hidden until session starts");
        }

        /// <summary>
        /// Fixes TMP_InputField viewport if not set in scene.
        /// TMP_InputField needs textViewport set to work properly.
        /// </summary>
        private void FixupInputFieldViewport()
        {
            if (numericInputField == null) return;
            
            // Use reflection to check and set textViewport if null
            var viewportField = typeof(TMP_InputField).GetField("m_TextViewport", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (viewportField != null)
            {
                var viewport = viewportField.GetValue(numericInputField) as RectTransform;
                if (viewport == null)
                {
                    // Find TextArea child which should be the viewport
                    var textArea = numericInputField.textComponent?.transform;
                    if (textArea != null)
                    {
                        viewportField.SetValue(numericInputField, textArea as RectTransform);
                        Debug.Log("[QuizUI] Fixed TMP_InputField textViewport");
                    }
                }
            }
        }

        private void SetupButtonListeners()
        {
            SetupMcqButtonListeners();
            SetupNumericListeners();
            SetupContinueListener();
        }
        
        private void SetupMcqButtonListeners()
        {
            for (int i = 0; i < mcqButtons.Length; i++)
            {
                int index = i;
                if (mcqButtons[i] != null)
                {
                    // Remove existing listeners first to avoid duplicates
                    mcqButtons[i].onClick.RemoveAllListeners();
                    mcqButtons[i].onClick.AddListener(() => OnMcqButtonClicked(index));
                }
            }
        }
        
        private void SetupNumericListeners()
        {
            if (numericSubmitButton != null)
            {
                numericSubmitButton.onClick.RemoveAllListeners();
                numericSubmitButton.onClick.AddListener(OnNumericSubmitClicked);
            }

            if (numericInputField != null)
            {
                numericInputField.onSubmit.RemoveAllListeners();
                numericInputField.onSubmit.AddListener(_ => OnNumericSubmitClicked());
            }
        }
        
        private void SetupContinueListener()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(OnContinueButtonClicked);
            }
        }
        
        /// <summary>
        /// Re-initializes button listeners after buttons are assigned via reflection.
        /// Call this from QuizSceneSetup after assigning mcqButtons.
        /// </summary>
        public void ReinitializeListeners()
        {
            Debug.Log("[QuizUI] Reinitializing button listeners...");
            SetupButtonListeners();
            int count = CountNonNullButtons();
            Debug.Log($"[QuizUI] MCQ buttons wired: {count} / {mcqButtons.Length}");
            
            if (count == 0)
            {
                Debug.LogError("[QuizUI] ERROR: No MCQ buttons found! Button clicks will not work.");
            }
            
            // Log button names for debugging
            for (int i = 0; i < mcqButtons.Length; i++)
            {
                if (mcqButtons[i] != null)
                {
                    Debug.Log($"[QuizUI]   Button[{i}]: {mcqButtons[i].name}");
                }
            }
        }
        
        private int CountNonNullButtons()
        {
            int count = 0;
            for (int i = 0; i < mcqButtons.Length; i++)
            {
                if (mcqButtons[i] != null) count++;
            }
            return count;
        }

        private void InitializePlayerBadges()
        {
            if (playerBadgeLeft != null)
                playerBadgeLeft.Initialize("YOU", new Color(0.08f, 0.72f, 0.65f)); // Teal

            if (playerBadgeRight != null)
                playerBadgeRight.Initialize("OPPONENT", new Color(0.98f, 0.45f, 0.09f)); // Orange
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

            if (continueButton != null)
                continueButton.onClick.RemoveAllListeners();
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        public void ShowMcqQuestion(McqQuestionData question, float customTimeLimit = 0f, string roundText = "Round 1: Multiple Choice")
        {
            if (question == null) return;

            // IMPORTANT: Enable quizPanel FIRST so Awake() runs before we show child panels
            ShowPanel(quizPanel, true);

            ClearPlayerLabels();

            currentQuestion = question;
            hasAnswered = false;
            waitingForContinue = false;

            if (questionText != null)
                questionText.text = question.QuestionText;

            if (categoryText != null)
                categoryText.text = question.Category ?? "";

            if (roundIndicatorText != null)
                roundIndicatorText.text = roundText;

            // Letter prefixes for answer options
            char[] letters = { 'A', 'B', 'C', 'D' };
            
            for (int i = 0; i < mcqButtons.Length && i < question.Choices.Length; i++)
            {
                if (mcqButtonTexts[i] != null)
                {
                    // Add letter prefix (A, B, C, D) to each option
                    string prefix = i < letters.Length ? $"{letters[i]}) " : "";
                    mcqButtonTexts[i].text = prefix + question.Choices[i];
                }

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

            // Show MCQ panel, hide others
            ShowPanel(mcqPanel, true);
            ShowPanel(numericPanel, false);
            ShowPanel(resultsPanel, false);

            // Hide continue button during question
            if (continueButton != null)
                continueButton.gameObject.SetActive(false);

            StartTimer(customTimeLimit > 0f ? customTimeLimit : defaultTimeLimit);
        }

        public void ShowNumericQuestion(NumericQuestionData question, float customTimeLimit = 0f, string roundText = "Round 2: Numeric")
        {
            if (question == null) return;

            // IMPORTANT: Enable quizPanel FIRST so Awake() runs before we show child panels
            ShowPanel(quizPanel, true);

            currentQuestion = question;
            hasAnswered = false;
            waitingForContinue = false;

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

            // Show Numeric panel, hide others
            ShowPanel(mcqPanel, false);
            ShowPanel(numericPanel, true);
            ShowPanel(resultsPanel, false);

            // Hide continue button during question
            if (continueButton != null)
                continueButton.gameObject.SetActive(false);

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
            float slideDistance = numericRect != null ? numericRect.rect.width : 800f;
            Vector2 startPos = new Vector2(slideDistance, 0f);
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
            waitingForContinue = false;

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
            // Ensure quizPanel is visible
            ShowPanel(quizPanel, true);
            
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
        /// Shows the final session result with Continue button.
        /// UI stays visible until Continue is clicked.
        /// </summary>
        public void ShowFinalResult(
            string playerAnswer,
            string opponentAnswer,
            float? playerError,
            float? opponentError,
            string correctAnswer,
            string winnerName,
            string decisionReason)
        {
            // Ensure quizPanel is visible
            ShowPanel(quizPanel, true);
            
            ShowPanel(mcqPanel, false);
            ShowPanel(numericPanel, false);
            ShowPanel(resultsPanel, true);

            string playerText = playerError.HasValue
                ? $"<b>YOU:</b> {playerAnswer} (error: {playerError.Value:F1})"
                : $"<b>YOU:</b> {playerAnswer}";

            string opponentText = opponentError.HasValue
                ? $"<b>OPPONENT:</b> {opponentAnswer} (error: {opponentError.Value:F1})"
                : $"<b>OPPONENT:</b> {opponentAnswer}";

            if (playerResultText != null)
                playerResultText.text = playerText;

            if (opponentResultText != null)
                opponentResultText.text = opponentText;

            if (correctAnswerText != null)
                correctAnswerText.text = $"Correct: {correctAnswer}";

            if (roundOutcomeText != null)
                roundOutcomeText.text = $"<size=130%><b>{winnerName}</b></size>\n{decisionReason}";

            // Show Continue button
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.interactable = true;
            }

            waitingForContinue = true;
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

        /// <summary>
        /// Returns true if the UI is waiting for Continue button click.
        /// </summary>
        public bool IsWaitingForContinue => waitingForContinue;

        public void HideQuiz()
        {
            isTimerRunning = false;
            waitingForContinue = false;
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

        /// <summary>
        /// Shows numeric answer reveal with result cards and badge updates.
        /// </summary>
        public IEnumerator ShowNumericRevealCoroutine(
            QuizAnswerResult playerAnswer,
            QuizAnswerResult opponentAnswer,
            float correctValue)
        {
            // Phase 1: Lock inputs (0.3s)
            if (numericInputField != null)
                numericInputField.interactable = false;
            if (numericSubmitButton != null)
                numericSubmitButton.interactable = false;

            yield return new WaitForSeconds(0.3f);

            // Phase 2: Show result cards (1.0s)
            string p1Text = playerAnswer.TimedOut
                ? "Timed Out!"
                : $"{playerAnswer.NumericValue:F1} (off by {playerAnswer.NumericDistance:F1})";
            string p2Text = opponentAnswer.TimedOut
                ? "Timed Out!"
                : $"{opponentAnswer.NumericValue:F1} (off by {opponentAnswer.NumericDistance:F1})";
            string outcome = DetermineNumericOutcome(playerAnswer, opponentAnswer);

            ShowRoundResults(p1Text, playerAnswer.IsCorrect, p2Text, opponentAnswer.IsCorrect, outcome);

            yield return new WaitForSeconds(1.0f);

            // Phase 3: Update status badges
            bool playerWon = DetermineNumericWinner(playerAnswer, opponentAnswer);

            if (playerBadgeLeft != null)
                playerBadgeLeft.SetState(playerWon ? BadgeState.ResultCorrect : BadgeState.ResultWrong);

            if (playerBadgeRight != null)
                playerBadgeRight.SetState(playerWon ? BadgeState.ResultWrong : BadgeState.ResultCorrect);

            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// Waits until Continue button is clicked. Use in coroutines.
        /// </summary>
        public IEnumerator WaitForContinueCoroutine()
        {
            waitingForContinue = true;

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.interactable = true;
            }

            while (waitingForContinue)
            {
                yield return null;
            }
        }

        private string DetermineNumericOutcome(QuizAnswerResult player, QuizAnswerResult opponent)
        {
            if (player.TimedOut && opponent.TimedOut)
                return "Both timed out!";
            if (player.TimedOut)
                return "You timed out - OPPONENT WINS!";
            if (opponent.TimedOut)
                return "Opponent timed out - YOU WIN!";

            if (player.NumericDistance < opponent.NumericDistance)
                return "YOU WIN! Closer answer!";
            if (opponent.NumericDistance < player.NumericDistance)
                return "OPPONENT WINS! Closer answer!";
            if (player.ResponseTimeSeconds < opponent.ResponseTimeSeconds)
                return "YOU WIN! Same distance, faster time!";
            return "OPPONENT WINS! Same distance, faster time!";
        }

        private bool DetermineNumericWinner(QuizAnswerResult player, QuizAnswerResult opponent)
        {
            if (player.TimedOut) return false;
            if (opponent.TimedOut) return true;
            if (player.NumericDistance < opponent.NumericDistance) return true;
            if (opponent.NumericDistance < player.NumericDistance) return false;
            return player.ResponseTimeSeconds <= opponent.ResponseTimeSeconds;
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
            hasAnswered = false; // Reset answered state when starting new timer
            UpdateTimerDisplay();
            Debug.Log($"[QuizUI] Timer started: {duration}s, isTimerRunning=true");
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
            Debug.Log($"[QuizUI] MCQ Button {index} clicked! hasAnswered={hasAnswered}, isTimerRunning={isTimerRunning}");
            
            if (hasAnswered)
            {
                Debug.Log("[QuizUI] Ignoring click - already answered");
                return;
            }
            
            if (!isTimerRunning)
            {
                Debug.Log("[QuizUI] Ignoring click - timer not running (no active session?)");
                return;
            }

            hasAnswered = true;
            isTimerRunning = false;
            MarkPlayerAnswered();

            float responseTime = GetCurrentResponseTime();
            Debug.Log($"[QuizUI] Player answered! Index={index}, ResponseTime={responseTime:F2}s");

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

        private void OnContinueButtonClicked()
        {
            Debug.Log("[QuizUI] Continue button clicked");
            waitingForContinue = false;

            if (continueButton != null)
                continueButton.gameObject.SetActive(false);

            OnContinueClicked?.Invoke();
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

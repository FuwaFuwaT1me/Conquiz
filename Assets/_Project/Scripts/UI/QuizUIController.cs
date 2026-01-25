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

            ShowPanel(mcqPanel, false);
            ShowPanel(numericPanel, true);
            ShowPanel(resultsPanel, false);
            ShowPanel(quizPanel, true);

            StartTimer(customTimeLimit > 0f ? customTimeLimit : defaultTimeLimit);
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
        }

        private void OnMcqButtonClicked(int index)
        {
            if (hasAnswered || !isTimerRunning) return;

            hasAnswered = true;
            isTimerRunning = false;

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
    }
}
